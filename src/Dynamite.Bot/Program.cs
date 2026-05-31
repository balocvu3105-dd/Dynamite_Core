using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Dynamite.Application;
using Dynamite.Bot.Services;
using Dynamite.Bot.Settings;
using Dynamite.Infrastructure;
using Dynamite.Modules.Moderation.Services;
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
        }));
        services.AddSingleton<InteractionService>(provider =>
        {
            var client = provider.GetRequiredService<DiscordSocketClient>();
            return new InteractionService(client);
        });
        services.AddTransient<ModLogService>();
        services.AddHostedService<BotHostedService>();
    })
    .Build();

await host.RunAsync();
