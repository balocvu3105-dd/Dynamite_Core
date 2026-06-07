// src/Dynamite.Bot/Services/BotHostedService.cs
namespace Dynamite.Bot.Services;

using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Bot.Settings;
using Dynamite.Modules.Logging;
using Dynamite.Modules.RoleManagement.Services;
using Dynamite.Modules.Security;
using Dynamite.Modules.Welcome;
using Microsoft.Extensions.DependencyInjection;
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

    private static readonly IReadOnlyList<Assembly> ModuleAssemblies =
    [
        Assembly.GetExecutingAssembly(),
        typeof(Dynamite.Modules.Moderation.Modules.ModerationModule).Assembly,
        typeof(Dynamite.Modules.Moderation.Modules.ConfigModule).Assembly,
        typeof(Dynamite.Modules.RoleManagement.Modules.AutoRoleModule).Assembly,
        typeof(Dynamite.Modules.Logging.Modules.LogConfigModule).Assembly,
        typeof(Dynamite.Modules.Welcome.Modules.WelcomeConfigModule).Assembly,
        typeof(Dynamite.Modules.Security.Modules.AntiSpamConfigModule).Assembly,
        typeof(Dynamite.Modules.Setup.SetupModule).Assembly,
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
        _client.UserJoined += OnUserJoinedAsync;
        _client.ButtonExecuted += OnButtonExecutedAsync;
        _client.SelectMenuExecuted += OnSelectMenuExecutedAsync;
        _client.ModalSubmitted += OnModalSubmittedAsync;

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
        var distinct = ModuleAssemblies.Distinct();
        foreach (var assembly in distinct)
        {
            await _interactions.AddModulesAsync(assembly, _services);
            _logger.LogDebug("Loaded interaction modules from {Assembly}", assembly.GetName().Name);
        }

        // Phase 6
        _services.GetRequiredService<LoggingEventHandler>().Subscribe();

        // Phase 7
        _services.GetRequiredService<WelcomeEventHandler>().Subscribe();

        // Phase 8
        _services.GetRequiredService<SecurityEventHandler>().Subscribe();

#if DEBUG
        await _interactions.RegisterCommandsToGuildAsync(_settings.TestGuildId);
        _logger.LogInformation("Commands registered to test guild {GuildId}", _settings.TestGuildId);
#else
        await _interactions.RegisterCommandsGloballyAsync();
        _logger.LogInformation("Commands registered globally");
#endif

        _logger.LogInformation("Bot is ready!");
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        using var scope = _services.CreateScope();
        var autoRoleService = scope.ServiceProvider.GetRequiredService<IAutoRoleService>();

        var roleIds = (await autoRoleService.GetRoleIdsToApplyAsync(user.Guild.Id)).ToList();
        if (roleIds.Count == 0) return;

        try
        {
            await user.AddRolesAsync(roleIds);
            _logger.LogInformation("Applied {Count} auto roles to user {UserId} in guild {GuildId}",
                roleIds.Count, user.Id, user.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply auto roles to user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
    }

    private async Task OnButtonExecutedAsync(SocketMessageComponent interaction)
    {
        var customId = interaction.Data.CustomId;

        if (customId == VerifyInteractionService.VerifyButtonId)
        {
            var service = _services.GetRequiredService<VerifyInteractionService>();
            await service.HandleVerifyAsync(interaction);
            return;
        }

        if (customId.StartsWith(RolePanelInteractionService.ButtonPrefix))
        {
            var service = _services.GetRequiredService<RolePanelInteractionService>();
            await service.HandleButtonAsync(interaction);
            return;
        }

        var ctx = new SocketInteractionContext<SocketMessageComponent>(_client, interaction);
        await _interactions.ExecuteCommandAsync(ctx, _services);
    }

    private async Task OnSelectMenuExecutedAsync(SocketMessageComponent interaction)
    {
        if (interaction.Data.CustomId.StartsWith(RolePanelInteractionService.SelectPrefix))
        {
            var service = _services.GetRequiredService<RolePanelInteractionService>();
            await service.HandleSelectAsync(interaction);
            return;
        }

        var ctx = new SocketInteractionContext<SocketMessageComponent>(_client, interaction);
        await _interactions.ExecuteCommandAsync(ctx, _services);
    }

    private async Task OnModalSubmittedAsync(SocketModal modal)
    {
        var ctx = new SocketInteractionContext<SocketModal>(_client, modal);
        await _interactions.ExecuteCommandAsync(ctx, _services);
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