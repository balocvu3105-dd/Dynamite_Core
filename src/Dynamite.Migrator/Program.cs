// src/Dynamite.Migrator/Program.cs
// Console app chạy một lần: apply tất cả pending EF migrations rồi exit.
// Docker Compose dùng condition: service_completed_successfully để
// đảm bảo bot và API chỉ start sau khi DB schema đã sẵn sàng.

using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration
            .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection is required.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting database migration...");

    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try { await db.Database.ExecuteSqlRawAsync("INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260630000000_AddUserBlacklist', '8.0.0') ON CONFLICT DO NOTHING;"); } catch {}
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Warnings\" ADD COLUMN IF NOT EXISTS \"TargetUsername\" varchar(100) NOT NULL DEFAULT ''; ALTER TABLE \"Warnings\" ADD COLUMN IF NOT EXISTS \"ModeratorUsername\" varchar(100) NOT NULL DEFAULT ''; ALTER TABLE \"ModerationActions\" ADD COLUMN IF NOT EXISTS \"TargetUsername\" varchar(100) NOT NULL DEFAULT ''; ALTER TABLE \"ModerationActions\" ADD COLUMN IF NOT EXISTS \"ModeratorUsername\" varchar(100) NOT NULL DEFAULT '';"); } catch {}

    var pending = await db.Database.GetPendingMigrationsAsync();
    var pendingList = pending.ToList();

    if (pendingList.Count == 0)
    {
        logger.LogInformation("No pending migrations. Database is up to date.");
    }
    else
    {
        logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
            pendingList.Count,
            string.Join(", ", pendingList));

        await db.Database.MigrateAsync();
        logger.LogInformation("Migration completed successfully.");
    }

    return 0;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Migration failed. Aborting.");
    return 1;
}
