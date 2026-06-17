// src/Dynamite.Modules.Economy/Services/LeaderboardHostedService.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Discord.WebSocket;
using Dynamite.Modules.Economy.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// IHostedService chạy nền — mỗi phút kiểm tra nếu là Chủ Nhật 12:00 UTC:
/// 1. Lấy top 10 từng loại leaderboard (Fishing / Chat / Voice) cho mỗi guild
/// 2. Tính delta rank so với tuần trước
/// 3. Lưu snapshot vào DB
/// 4. Reset WeeklyActivity counters
/// 5. (Optional) Post embed thông báo lên channel nếu config
/// </summary>
public class LeaderboardHostedService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient  _discord;
    private readonly ILogger<LeaderboardHostedService> _logger;

    // Theo dõi đã chụp snapshot tuần này chưa (reset mỗi tuần)
    // Key: guildId, Value: last snapshot date (Sunday)
    private readonly Dictionary<ulong, DateTime> _lastSnapshotDate = [];

    public LeaderboardHostedService(
        IServiceScopeFactory scopeFactory,
        DiscordSocketClient  discord,
        ILogger<LeaderboardHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _discord      = discord;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LeaderboardHostedService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSnapshotAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LeaderboardHostedService");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAndSnapshotAsync()
    {
        var now = DateTime.UtcNow;

        // Chủ Nhật, 12:00–12:01 UTC
        if (now.DayOfWeek != DayOfWeek.Sunday || now.Hour != 12 || now.Minute != 0)
            return;

        var snapshotDate = now.Date;

        using var scope  = _scopeFactory.CreateScope();
        var lbRepo   = scope.ServiceProvider.GetRequiredService<ILeaderboardRepository>();
        var pondRepo = scope.ServiceProvider.GetRequiredService<IPondRepository>();

        var allPonds = await pondRepo.GetAllAsync();

        foreach (var pond in allPonds)
        {
            // Idempotency: chỉ chạy 1 lần cho mỗi guild mỗi tuần
            if (_lastSnapshotDate.TryGetValue(pond.GuildId, out var last)
                && last.Date == snapshotDate)
                continue;

            try
            {
                await TakeSnapshotForGuildAsync(pond.GuildId, snapshotDate, lbRepo);
                _lastSnapshotDate[pond.GuildId] = snapshotDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to take leaderboard snapshot for guild {GuildId}", pond.GuildId);
            }
        }
    }

    private async Task TakeSnapshotForGuildAsync(
        ulong guildId, DateTime weekDate,
        ILeaderboardRepository lbRepo)
    {
        foreach (var lbType in Enum.GetValues<LeaderboardType>())
        {
            // 1. Lấy top 10 tuần này
            var top = await lbRepo.GetTopWeeklyAsync(guildId, lbType, top: 10);
            if (top.Count == 0) continue;

            // 2. Lấy snapshot tuần trước để tính delta rank
            var prevSnapshot = await lbRepo.GetPreviousSnapshotAsync(guildId, lbType)
                            ?? await lbRepo.GetLatestSnapshotAsync(guildId, lbType);

            var prevRanks = prevSnapshot?.Entries
                .ToDictionary(e => e.UserId, e => e.Rank)
                ?? new Dictionary<ulong, int>();

            // 3. Build snapshot
            var snapshot = new LeaderboardSnapshot
            {
                GuildId       = guildId,
                Type          = lbType,
                WeekStartDate = weekDate,
                CreatedAt     = DateTime.UtcNow
            };

            for (var i = 0; i < top.Count; i++)
            {
                var activity = top[i];
                var rank     = i + 1;
                var value    = lbType switch
                {
                    LeaderboardType.Fishing => activity.WeeklyFishCaught,
                    LeaderboardType.Chat    => activity.WeeklyMessages,
                    LeaderboardType.Voice   => activity.WeeklyVoiceMinutes,
                    _ => 0
                };
                var delta = prevRanks.TryGetValue(activity.UserId, out var prevRank)
                    ? prevRank - rank  // dương = leo rank, âm = tụt rank
                    : 0;               // mới vào = không có delta

                snapshot.Entries.Add(new LeaderboardEntry
                {
                    GuildId   = guildId,
                    UserId    = activity.UserId,
                    Rank      = rank,
                    Value     = value,
                    DeltaRank = delta,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await lbRepo.AddSnapshotAsync(snapshot);
            _logger.LogInformation(
                "Leaderboard snapshot [{Type}] saved for guild {GuildId} — {Count} entries",
                lbType, guildId, snapshot.Entries.Count);
        }

        // 4. Reset weekly counters sau khi đã snapshot
        await lbRepo.ResetWeeklyActivitiesAsync(guildId);
        await lbRepo.SaveChangesAsync();

        // 5. Announce (optional)
        await AnnounceLeaderboardAsync(guildId, weekDate, lbRepo);
    }

    private async Task AnnounceLeaderboardAsync(
        ulong guildId, DateTime weekDate,
        ILeaderboardRepository lbRepo)
    {
        var guild = _discord.GetGuild(guildId);
        if (guild is null) return;

        // Load guild config để lấy channel IDs
        using var scope     = _scopeFactory.CreateScope();
        var configRepo      = scope.ServiceProvider.GetRequiredService<IGuildConfigRepository>();
        var config          = await configRepo.GetByGuildIdAsync(guildId);

        var weekLabel = weekDate.ToString("dd/MM/yyyy");

        // ── Bảng Ngư Dân → FishingLeaderboardChannelId ──────────────────────
        var fishingSnap = await lbRepo.GetLatestSnapshotAsync(guildId, LeaderboardType.Fishing);
        if (fishingSnap is not null)
        {
            var fishingChannelId = config?.FishingLeaderboardChannelId;
            var fishingChannel   = fishingChannelId.HasValue
                ? guild.GetTextChannel(fishingChannelId.Value)
                : guild.SystemChannel as ITextChannel;

            if (fishingChannel is not null)
            {
                var excluded = GetPrivilegedIds(guild);
                var embed = EconomyEmbedBuilder.BuildLeaderboardEmbed(
                    fishingSnap, guild, weekLabel, excluded);
                await TrySendAsync(fishingChannel, embed, guildId, "Fishing");
            }
        }

        // ── Bảng Server (Chat + Voice) → ServerLeaderboardChannelId ─────────
        var serverSnaps = new List<LeaderboardSnapshot>();
        foreach (var t in new[] { LeaderboardType.Chat, LeaderboardType.Voice })
        {
            var snap = await lbRepo.GetLatestSnapshotAsync(guildId, t);
            if (snap is not null) serverSnaps.Add(snap);
        }

        if (serverSnaps.Count > 0)
        {
            var serverChannelId = config?.ServerLeaderboardChannelId;
            var serverChannel   = serverChannelId.HasValue
                ? guild.GetTextChannel(serverChannelId.Value)
                : guild.SystemChannel as ITextChannel;

            if (serverChannel is not null)
            {
                var excluded = GetPrivilegedIds(guild);
                foreach (var snap in serverSnaps)
                {
                    var embed = EconomyEmbedBuilder.BuildLeaderboardEmbed(
                        snap, guild, weekLabel, excluded);
                    await TrySendAsync(serverChannel, embed, guildId, snap.Type.ToString());
                }
            }
        }
    }

    private async Task TrySendAsync(
        ITextChannel channel, Embed embed, ulong guildId, string label)
    {
        try
        {
            await channel.SendMessageAsync(embed: embed);
            _logger.LogInformation(
                "[Leaderboard] Posted {Label} to channel #{Channel} in guild {GuildId}",
                label, channel.Name, guildId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Leaderboard] Failed to post {Label} for guild {GuildId}", label, guildId);
        }
    }

    private static HashSet<ulong> GetPrivilegedIds(SocketGuild guild)
    {
        var ids = new HashSet<ulong> { guild.OwnerId };
        foreach (var member in guild.Users)
            if (member.GuildPermissions.Administrator)
                ids.Add(member.Id);
        return ids;
    }

    private static string MedalEmoji(int rank) => rank switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $"#{rank}"
    };
}
