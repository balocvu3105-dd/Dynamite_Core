// src/Dynamite.API/Program.cs
using System.Text;
using Dynamite.API.Auth;
using Dynamite.API.HealthChecks;
using Dynamite.API.Middleware;
using Dynamite.API.Services;
using Dynamite.Application;
using Dynamite.Infrastructure;
using Dynamite.Infrastructure.Persistence;
using Dynamite.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ─── Graceful Shutdown Timeout ────────────────────────────────────────────────
// Default là 5s — tăng lên 30s để các request đang xử lý có thời gian hoàn thành
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// ─── Application + Infrastructure layers ─────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();

// ─── CORS ────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("CRITICAL SECURITY ERROR: 'Jwt:Secret' is not configured in environment variables for Production environment!");
    }
    jwtSecret = "Dynamite@Secret#Key$2026!Dashboard%API&Secure";
}
if (jwtSecret.Length < 32) jwtSecret = jwtSecret.PadRight(32, 'X');
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Dynamite";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DynamiteDashboard";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                var cookieToken = context.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(cookieToken))
                    context.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ─── App Services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpClient<DiscordOAuthService>();
builder.Services.AddScoped<GuildAuthorizationService>();

// Phase 9b
builder.Services.AddScoped<GuildPresenceService>();

// ─── IBotStatusProvider ───────────────────────────────────────────────────────
// Trong môi trường API-standalone (không chạy cùng Bot process),
// dùng NullBotStatusProvider — luôn trả về false.
// Khi chạy cùng Bot (single process), Bot sẽ override bằng BotStatusProvider.
//
// NOTE: Nếu sau này API và Bot tách ra 2 process riêng, thay thế bằng
// một implementation đọc từ Redis/shared cache để sync status cross-process.
builder.Services.AddSingleton<IBotStatusProvider, NullBotStatusProvider>();

// ─── Health Checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "db"])
    .AddCheck<BotHealthCheck>(
        name: "discord-bot",
        failureStatus: HealthStatus.Degraded,   // bot not ready = degraded, not unhealthy
        tags: ["ready", "bot"]);

builder.Services.AddTransient<Dynamite.Modules.Setup.Services.SmartSetupEngine>();
builder.Services.AddScoped<Dynamite.Modules.Economy.Services.WalletService>();

// ─── Controllers ─────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ─── Swagger ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dynamite Dashboard API",
        Version = "v1",
        Description = "REST API for Dynamite Discord Bot Dashboard"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste JWT access token here"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dynamite API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("DashboardPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ─── Health Endpoints ─────────────────────────────────────────────────────────

// Liveness: API process còn sống không?
// Trả về 200 ngay lập tức — không check DB hay bot.
// Docker/K8s dùng để quyết định có restart container không.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,   // không run check nào — chỉ confirm process alive
    ResponseWriter = WriteHealthResponse
});

// Readiness: Hệ thống sẵn sàng nhận traffic chưa?
// Check DB connection + bot status.
// Docker/K8s dùng để quyết định có route traffic vào không.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

app.MapControllers();

app.Run();

// ─── Health Response Writer ───────────────────────────────────────────────────
static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var result = new
    {
        status = report.Status.ToString(),
        duration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.TotalMilliseconds,
            data = e.Value.Data.Count > 0 ? e.Value.Data : null
        })
    };

    return context.Response.WriteAsync(
        JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        }));
}