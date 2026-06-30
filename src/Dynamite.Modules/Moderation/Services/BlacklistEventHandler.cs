// src/Dynamite.Modules/Moderation/Services/BlacklistEventHandler.cs
namespace Dynamite.Modules.Moderation.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Listens for UserJoined events and auto-bans any user that is on
/// the guild's permanent blacklist before they can interact with anything.
///
/// Design note: We use IServiceScopeFactory (singleton-safe) to create a
/// per-event DI scope, matching the same pattern used by WelcomeEventHandler.
/// The bot's DI root is a singleton so we cannot inject scoped services directly.
/// </summary>
public class BlacklistEventHandler
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlacklistEventHandler> _logger;

    // Reuse Discord's Bot user ID as the "moderator" for auto-ban audit entries.
    // Resolved lazily on first use (after bot is Ready).
    private ulong _botUserId;

    public BlacklistEventHandler(
        DiscordSocketClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<BlacklistEventHandler> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Subscribe()
    {
        _client.UserJoined += user => SafeRun(() => OnUserJoinedAsync(user));
        _logger.LogInformation("BlacklistEventHandler subscribed");
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        using var scope = _scopeFactory.CreateScope();
        var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

        var isBlacklisted = await blacklistService.IsBlacklistedAsync(user.Guild.Id, user.Id);
        if (!isBlacklisted)
            return;

        // Resolve bot user ID once and cache it.
        if (_botUserId == 0)
            _botUserId = _client.CurrentUser?.Id ?? 0;

        const string reason = "[Auto] Blacklisted user — rejoined and was automatically banned.";

        try
        {
            await user.Guild.AddBanAsync(user.Id, 0, reason);

            _logger.LogWarning(
                "Blacklisted user {UserId} ({Username}) tried to rejoin guild {GuildId} — auto-banned.",
                user.Id, user.Username, user.Guild.Id);
        }
        catch (Exception ex)
        {
            // Log but don't rethrow — we don't want to crash other UserJoined handlers.
            _logger.LogError(ex,
                "Failed to auto-ban blacklisted user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
    }

    private Task SafeRun(Func<Task> handler)
    {
        _ = Task.Run(async () =>
        {
            try { await handler(); }
            catch (Exception ex) { _logger.LogError(ex, "Unhandled exception in BlacklistEventHandler"); }
        });
        return Task.CompletedTask;
    }
}
