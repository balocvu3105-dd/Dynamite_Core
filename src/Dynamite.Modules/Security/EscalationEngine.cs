// src/Dynamite.Modules/Security/EscalationEngine.cs
namespace Dynamite.Modules.Security;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Security.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class EscalationEngine
{
    private readonly ViolationTracker _tracker;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EscalationEngine> _logger;

    // Escalation thresholds
    private const int WarnThreshold = 3;
    private const int TimeoutThreshold = 5;
    private const int BanThreshold = 5; // > 5 = ban

    private static readonly TimeSpan TimeoutDuration = TimeSpan.FromMinutes(10);

    public EscalationEngine(
        ViolationTracker tracker,
        IServiceScopeFactory scopeFactory,
        ILogger<EscalationEngine> logger)
    {
        _tracker = tracker;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Called when a violation is detected. Deletes the message,
    /// increments violation count, and applies appropriate punishment.
    /// </summary>
    public async Task HandleViolationAsync(
        SocketMessage message, string violationType)
    {
        if (message.Author is not SocketGuildUser guildUser) return;
        if (message.Channel is not SocketTextChannel channel) return;

        var guildId = guildUser.Guild.Id;
        var userId = guildUser.Id;

        // Delete the offending message
        try { await message.DeleteAsync(); }
        catch { /* already deleted */ }

        var violations = _tracker.IncrementViolation(guildId, userId);

        _logger.LogInformation(
            "Violation [{Type}] for user {UserId} in guild {GuildId} — count: {Count}",
            violationType, userId, guildId, violations);

        if (violations > BanThreshold)
        {
            await ApplyBanAsync(guildUser, channel, violations);
        }
        else if (violations == TimeoutThreshold)
        {
            await ApplyTimeoutAsync(guildUser, channel, violations);
        }
        else if (violations == WarnThreshold)
        {
            await ApplyWarnAsync(guildUser, channel, violations);
        }
        else
        {
            // Strike 1-2: ephemeral-style warning in channel (auto-delete after 5s)
            await SendTemporaryWarningAsync(channel,
                SecurityEmbeds.SpamWarning(guildUser.DisplayName, violations));
        }
    }

    private async Task ApplyWarnAsync(
        SocketGuildUser user, SocketTextChannel channel, int violations)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();

            await moderationService.WarnAsync(
                user.Guild.Id,
                user.Guild.Name,
                user.Id,
                channel.Guild.CurrentUser.Id,
                $"[AutoMod] Spam/violation detected (strike {violations})");

            await SendTemporaryWarningAsync(channel, SecurityEmbeds.Build(
                "⚠️ Warning Issued",
                $"{user.Mention} has been warned for repeated violations.",
                new Color(0xFEE75C)));

            _logger.LogInformation("AutoMod warned user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply warn to user {UserId}", user.Id);
        }
    }

    private async Task ApplyTimeoutAsync(
        SocketGuildUser user, SocketTextChannel channel, int violations)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();

            await user.SetTimeOutAsync(TimeoutDuration);

            await moderationService.TimeoutAsync(
                user.Guild.Id,
                user.Guild.Name,
                user.Id,
                channel.Guild.CurrentUser.Id,
                $"[AutoMod] Repeated violations (strike {violations})",
                TimeoutDuration);

            await SendTemporaryWarningAsync(channel, SecurityEmbeds.Build(
                "🔇 User Timed Out",
                $"{user.Mention} has been timed out for 10 minutes due to repeated violations.",
                new Color(0xED4245)));

            _logger.LogInformation("AutoMod timed out user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply timeout to user {UserId}", user.Id);
        }
    }

    private async Task ApplyBanAsync(
        SocketGuildUser user, SocketTextChannel channel, int violations)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();

            await user.Guild.AddBanAsync(user.Id,
                reason: $"[AutoMod] Excessive violations (strike {violations})");

            await moderationService.BanAsync(
                user.Guild.Id,
                user.Guild.Name,
                user.Id,
                channel.Guild.CurrentUser.Id,
                $"[AutoMod] Excessive violations (strike {violations})");

            await SendTemporaryWarningAsync(channel, SecurityEmbeds.Build(
                "🔨 User Banned",
                $"A user has been banned by AutoMod for excessive violations.",
                new Color(0xED4245)));

            _tracker.ResetViolations(user.Guild.Id, user.Id);

            _logger.LogInformation("AutoMod banned user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply ban to user {UserId}", user.Id);
        }
    }

    // Gửi warning rồi tự xóa sau 5 giây — giảm channel clutter
    private static async Task SendTemporaryWarningAsync(ITextChannel channel, Embed embed)
    {
        try
        {
            var msg = await channel.SendMessageAsync(embed: embed);
            _ = Task.Delay(TimeSpan.FromSeconds(5))
                .ContinueWith(_ => msg.DeleteAsync());
        }
        catch { /* non-critical */ }
    }
}