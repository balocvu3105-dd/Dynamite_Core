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
await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        await _interactions.AddModulesAsync(typeof(Dynamite.Modules.Moderation.Modules.ModerationModule).Assembly, _services);

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
            LogSeverity.Error    => LogLevel.Error,
            LogSeverity.Warning  => LogLevel.Warning,
            LogSeverity.Info     => LogLevel.Information,
            _                    => LogLevel.Debug
        };
        _logger.Log(level, log.Exception, "[Discord] {Message}", log.Message);
        return Task.CompletedTask;
    }
}
