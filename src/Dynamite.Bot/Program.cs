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
using Dynamite.Modules.Giveaway.Commands;
using Dynamite.Modules.Giveaway.Interactions;
using Dynamite.Modules.Giveaway.Services;
using Dynamite.Modules.Logging;
using Dynamite.Modules.Logging.Loggers;
using Dynamite.Modules.Moderation.Services;
using Dynamite.Modules.RoleManagement.Helpers;
using Dynamite.Modules.RoleManagement.Services;
using Dynamite.Modules.Security;
using Dynamite.Modules.Setup;
using Dynamite.Modules.Ticket.Commands;
using Dynamite.Modules.Ticket.Interactions;
using Dynamite.Modules.Ticket.Services;
using Dynamite.Modules.Welcome;
using Dynamite.Modules.Welcome.Helpers;
using Dynamite.Modules.Economy.Commands;
using Dynamite.Modules.Economy.Handlers;
using Dynamite.Modules.Economy.Services;
using Dynamite.Modules.Voice;
using Dynamite.Modules.Voice.Services;
using Dynamite.Shared;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, config) =>
    {
        config
            .MinimumLevel.Information()
            // Giữ override: EF Core log mọi query ở Information — quá ồn
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File("logs/dynamite-.txt", rollingInterval: RollingInterval.Day);
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddApplication();
        services.AddInfrastructure(config);
        services.AddMemoryCache(); // dùng cho WeatherService cache
        services.Configure<DiscordSettings>(config.GetSection("Discord"));

        // ─── Scheduled Restart ────────────────────────────────────────────────
        services.Configure<ScheduledRestartSettings>(
            config.GetSection("ScheduledRestart"));

        // ─── Graceful Shutdown Timeout ────────────────────────────────────────
        // Default là 5s — tăng lên 30s để StopAsync có đủ thời gian
        // gửi audit log notifications và drain các request đang xử lý
        services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(30);
        });

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

        // ─── Bot Status Provider ──────────────────────────────────────────────
        // Register as cả BotStatusProvider (concrete) lẫn IBotStatusProvider (interface)
        // Singleton vì cần share state giữa BotHostedService và bất kỳ consumer nào
        services.AddSingleton<BotStatusProvider>();
        services.AddSingleton<IBotStatusProvider>(sp =>
            sp.GetRequiredService<BotStatusProvider>());

        // Phase 2
        services.AddTransient<ModLogService>();

        // Phase 3
        services.AddScoped<IAutoRoleRepository, AutoRoleRepository>();
        services.AddScoped<IRolePanelRepository, RolePanelRepository>();
        services.AddScoped<IAutoRoleService, AutoRoleService>();
        services.AddScoped<IRolePanelService, RolePanelService>();
        services.AddSingleton<RolePanelInteractionService>();
        services.AddTransient<RolePanelBuilder>();

        // Phase 4
        services.AddTransient<SetupExecutor>();

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

        // Phase 9b
        services.AddSingleton<GuildPresenceSyncService>();

        // Phase 10a — Giveaway
        services.AddScoped<IGiveawayRepository, GiveawayRepository>();
        services.AddScoped<GiveawayService>();
        services.AddSingleton<GiveawayInteractionService>();
        services.AddHostedService<GiveawayTimerService>();

        // Phase 10b — Ticket
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<TicketService>();
        services.AddSingleton<TicketInteractionService>();

        // Phase 10c — Economy v2
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IPondRepository, PondRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<WalletService>();
        services.AddScoped<FishingService>();
        services.AddScoped<ShopService>();
        services.AddScoped<XpService>();
        services.AddScoped<PondService>();
        services.AddScoped<WeatherService>();
        services.AddSingleton<EconomyEventHandler>();

        // Economy v2.1 — Fish Bag, Special Pool, Leaderboard
        services.AddScoped<IFishBagRepository, FishBagRepository>();
        services.AddScoped<ISpecialPoolRepository, SpecialPoolRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
        services.AddScoped<FishBagService>();
        services.AddScoped<SpecialPoolService>();
        services.AddHostedService<SpecialPoolScheduler>();
        services.AddHostedService<LeaderboardHostedService>();

        // Economy v2.2 — Activity Log, Snapshot/Backup, Miss/Escape
        services.AddScoped<IFishingLogRepository, FishingLogRepository>();
        services.AddScoped<IFishingSnapshotRepository, FishingSnapshotRepository>();
        services.AddScoped<FishingSnapshotService>();
        services.AddHostedService<FishingBackupScheduler>();

        // Economy v2.3 — Trophy (Collector leaderboard) + Auto-Fish
        services.AddScoped<IFishTrophyRepository, FishTrophyRepository>();
        services.AddHostedService<AutoFishScheduler>();

        // Economy v2.4 — Fish Encyclopedia (/fishing dex)
        services.AddScoped<IFishEncyclopediaRepository, FishEncyclopediaRepository>();
        services.AddScoped<FishEncyclopediaService>();

        // Phase A — Channel System (Shop showcase, Invoice, Weather forecast, Guide)
        services.AddScoped<ShopShowcaseService>();
        services.AddScoped<InvoiceService>();
        services.AddSingleton<WeatherForecastService>(); // Singleton vì WeatherChangeNotifier inject trực tiếp
        services.AddScoped<GuideService>();

        // Phase A — Weather Change Notifier (mention role Ngư Dân khi thời tiết đổi)
        services.AddHostedService<WeatherChangeNotifier>();

        // Phase 5 — Temp Voice
        services.AddSingleton<TempVoiceService>();
        services.AddSingleton<TempVoiceEventHandler>();

        // ─── Phase E3 — Scheduled Restart ────────────────────────────────────────
        services.AddHostedService<ScheduledRestartService>();

        services.AddHostedService<BotHostedService>();
    })
    .Build();

await host.RunAsync();
