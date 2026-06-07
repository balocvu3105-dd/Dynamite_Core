// src/Dynamite.Bot/Program.cs
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Dynamite.Application;
using Dynamite.Application.Interfaces;
using Dynamite.Application.Services;
using Dynamite.Bot.Services;
using Dynamite.Bot.Settings;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure;
using Dynamite.Infrastructure.Repositories;
using Dynamite.Modules.Logging;
using Dynamite.Modules.Logging.Loggers;
using Dynamite.Modules.Moderation.Services;
using Dynamite.Modules.RoleManagement.Helpers;
using Dynamite.Modules.RoleManagement.Services;
using Dynamite.Modules.Security;
using Dynamite.Modules.Welcome;
using Dynamite.Modules.Welcome.Helpers;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, config) =>
    {
        config
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/dynamite-.txt", rollingInterval: RollingInterval.Day);
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddApplication();
        services.AddInfrastructure(config);
        services.Configure<DiscordSettings>(config.GetSection("Discord"));

        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.Guilds
                | GatewayIntents.GuildMembers
                | GatewayIntents.GuildMessages
                | GatewayIntents.MessageContent
                | GatewayIntents.GuildVoiceStates,
            MessageCacheSize = 1000
        }));

        services.AddSingleton<InteractionService>(provider =>
        {
            var client = provider.GetRequiredService<DiscordSocketClient>();
            return new InteractionService(client);
        });

        // Phase 2
        services.AddTransient<ModLogService>();

        // Phase 3
        services.AddScoped<IAutoRoleRepository, AutoRoleRepository>();
        services.AddScoped<IRolePanelRepository, RolePanelRepository>();
        services.AddScoped<IAutoRoleService, AutoRoleService>();
        services.AddScoped<IRolePanelService, RolePanelService>();
        services.AddSingleton<RolePanelInteractionService>();
        services.AddTransient<RolePanelBuilder>();

        // Phase 6
        services.AddSingleton<MessageLogger>();
        services.AddSingleton<MemberLogger>();
        services.AddSingleton<VoiceLogger>();
        services.AddSingleton<ServerLogger>();
        services.AddSingleton<LoggingEventHandler>();

        // Phase 7
        services.AddHttpClient<WelcomeImageGenerator>();
        services.AddSingleton<WelcomeImageGenerator>();
        services.AddSingleton<WelcomeEventHandler>();
        services.AddSingleton<VerifyInteractionService>();

        // Phase 8
        services.AddScoped<IAntiSpamRepository, AntiSpamRepository>();
        services.AddSingleton<ViolationTracker>();
        services.AddSingleton<EscalationEngine>();
        services.AddSingleton<SecurityEventHandler>();

        services.AddHostedService<BotHostedService>();
    })
    .Build();

await host.RunAsync();