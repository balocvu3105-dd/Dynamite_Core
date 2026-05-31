namespace Dynamite.Bot.Services;

using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Bot.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class BotHostedService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly DiscordSettings _settings;
    private readonly ILogger<BotHostedService> _logger;

    // Fix: enumerate all module assemblies explicitly instead of combining
    // Assembly.GetEntryAssembly() (which scans everything it can see) with a
    // specific module assembly. If the entry assembly ever picks up the module
    // assembly transitively, commands get registered twice and Discord.Net throws.
    private static readonly IReadOnlyList<Assembly> ModuleAssemblies =
    [
        Assembly.GetExecutingAssembly(),                                   // Dynamite.Bot
        typeof(Dynamite.Modules.Moderation.Modules.ModerationModule).Assembly,
        typeof(Dynamite.Modules.Moderation.Modules.ConfigModule).Assembly,
        // Add new module assemblies here as they are created.
        // Example: typeof(Dynamite.Modules.Welcome.Modules.WelcomeModule).Assembly,
    ];

    public BotHostedService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IOptions<DiscordSettings> settings,
        ILogger<BotHostedService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;

        await _client.LoginAsync(TokenType.Bot, _settings.Token);
        await _client.StartAsync();

        _logger.LogInformation("Bot started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        _logger.LogInformation("Bot stopped");
    }

    private async Task OnReadyAsync()
    {
        // Deduplicate by assembly identity before scanning to be safe.
        var distinct = ModuleAssemblies.Distinct();
        foreach (var assembly in distinct)
        {
            await _interactions.AddModulesAsync(assembly, _services);
            _logger.LogDebug("Loaded interaction modules from {Assembly}", assembly.GetName().Name);
        }

#if DEBUG
        await _interactions.RegisterCommandsToGuildAsync(_settings.TestGuildId);
        _logger.LogInformation("Commands registered to test guild {GuildId}", _settings.TestGuildId);
#else
        await _interactions.RegisterCommandsGloballyAsync();
        _logger.LogInformation("Commands registered globally");
#endif

        _logger.LogInformation("Bot is ready!");
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_client, interaction);
        await _interactions.ExecuteCommandAsync(ctx, _services);
    }

    private Task LogAsync(LogMessage log)
    {
        var level = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            _ => LogLevel.Debug
        };
        _logger.Log(level, log.Exception, "[Discord] {Message}", log.Message);
        return Task.CompletedTask;
    }
}