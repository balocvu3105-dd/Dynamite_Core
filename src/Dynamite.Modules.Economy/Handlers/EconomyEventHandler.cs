// src/Dynamite.Modules.Economy/Handlers/EconomyEventHandler.cs
namespace Dynamite.Modules.Economy.Handlers;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Subscribe vào MessageReceived và VoiceStateUpdated để award server XP.
/// Singleton — Subscribe() được gọi một lần trong OnReadyAsync.
/// Dùng IServiceScopeFactory để tạo scope per-event (XpService là Scoped).
/// </summary>
public class EconomyEventHandler
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EconomyEventHandler> _logger;

    private bool _subscribed = false;

    public EconomyEventHandler(
        DiscordSocketClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<EconomyEventHandler> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Subscribe()
    {
        if (_subscribed) return;
        _client.MessageReceived     += OnMessageReceivedAsync;
        _client.UserVoiceStateUpdated += OnVoiceStateUpdatedAsync;
        _subscribed = true;
        _logger.LogInformation("EconomyEventHandler subscribed");
    }

    // ── Chat XP ──────────────────────────────────────────────────────────────

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        // Bỏ qua bot và DM
        if (message.Author.IsBot) return;
        if (message is not SocketUserMessage userMsg) return;
        if (userMsg.Channel is not SocketGuildChannel guildChannel) return;

        var guildId = guildChannel.Guild.Id;
        var userId  = message.Author.Id;

        try
        {
            using var scope  = _scopeFactory.CreateScope();
            var xp    = scope.ServiceProvider.GetRequiredService<XpService>();
            var lbRepo = scope.ServiceProvider.GetRequiredService<ILeaderboardRepository>();

            // Chat XP
            var result = await xp.AwardChatXpAsync(guildId, userId);

            // Weekly leaderboard counter
            var activity = await lbRepo.GetOrCreateWeeklyActivityAsync(guildId, userId);
            activity.WeeklyMessages++;
            await lbRepo.SaveChangesAsync();

            if (result is { LeveledUp: true })
                await NotifyLevelUpAsync(guildChannel, message.Author, "Server", result.NewLevel, result.RoleAwarded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EconomyEventHandler: chat XP failed for user {UserId}", userId);
        }
    }

    // ── Voice XP ─────────────────────────────────────────────────────────────

    private async Task OnVoiceStateUpdatedAsync(
        SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.IsBot) return;

        // Tìm guild từ channel (trước hoặc sau)
        var guildChannel = (before.VoiceChannel ?? after.VoiceChannel) as SocketGuildChannel;
        if (guildChannel is null) return;

        var guildId = guildChannel.Guild.Id;
        var userId  = user.Id;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var xp = scope.ServiceProvider.GetRequiredService<XpService>();

            // Join voice
            if (before.VoiceChannel is null && after.VoiceChannel is not null)
            {
                await xp.OnVoiceJoinAsync(guildId, userId);
                return;
            }

            // Leave voice (bao gồm cả chuyển channel — tính XP rồi reset timer)
            if (before.VoiceChannel is not null)
            {
                var result = await xp.OnVoiceLeaveAsync(guildId, userId);

                // Track voice minutes for leaderboard
                if (result is { VoiceMinutesAwarded: > 0 })
                {
                    var lbRepo   = scope.ServiceProvider.GetRequiredService<ILeaderboardRepository>();
                    var activity = await lbRepo.GetOrCreateWeeklyActivityAsync(guildId, userId);
                    activity.WeeklyVoiceMinutes += result.VoiceMinutesAwarded;
                    await lbRepo.SaveChangesAsync();
                }

                // Nếu chuyển sang channel khác → re-join
                if (after.VoiceChannel is not null)
                    await xp.OnVoiceJoinAsync(guildId, userId);

                if (result is { LeveledUp: true } && after.VoiceChannel is SocketGuildChannel targetChannel)
                    await NotifyLevelUpAsync(targetChannel, user, "Server", result.NewLevel, result.RoleAwarded);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EconomyEventHandler: voice XP failed for user {UserId}", userId);
        }
    }

    // ── Level-up notification (DM only) ─────────────────────────────────────

    /// <summary>
    /// Gửi DM thông báo level up cho chính user đó — không spam channel công khai.
    /// Nếu user đóng DM → silently ignore.
    /// </summary>
    private static async Task NotifyLevelUpAsync(
        IChannel _, IUser user, string poolName, int newLevel, ulong? roleId)
    {
        var roleText = roleId.HasValue ? $"\n🎖️ Bạn nhận được role mới: <@&{roleId.Value}>!" : "";
        var embed = new EmbedBuilder()
            .WithTitle("🎉 Level Up!")
            .WithDescription(
                $"Chúc mừng! Bạn đã đạt **{poolName} Level {newLevel}**!{roleText}\n\n" +
                $"Dùng `/level` để xem tiến trình đầy đủ.")
            .WithColor(new Color(0xF5A623u))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        try
        {
            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(embed: embed);
        }
        catch { /* User đóng DM — bỏ qua */ }
    }
}
