// src/Dynamite.Modules.Economy/Services/LeaderboardHostedService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Discord.WebSocket;
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
        if (guild?.SystemChannel is null) return;

        var snapshots = new List<LeaderboardSnapshot>();
        foreach (var lbType in Enum.GetValues<LeaderboardType>())
        {
            var snap = await lbRepo.GetLatestSnapshotAsync(guildId, lbType);
            if (snap is not null) snapshots.Add(snap);
        }

        if (snapshots.Count == 0) return;

        var embed = new Discord.EmbedBuilder()
            .WithTitle($"🏆 Bảng Xếp Hạng Tuần — {weekDate:dd/MM/yyyy}")
            .WithColor(Discord.Color.Gold)
            .WithFooter("Cập nhật mỗi Chủ Nhật 12:00 UTC")
            .WithCurrentTimestamp();

        foreach (var snap in snapshots)
        {
            var icon = snap.Type switch
            {
                LeaderboardType.Fishing => "🎣",
                LeaderboardType.Chat    => "💬",
                LeaderboardType.Voice   => "🎙️",
                _ => "📊"
            };
            var unit = snap.Type switch
            {
                LeaderboardType.Fishing => "cá",
                LeaderboardType.Chat    => "tin nhắn",
                LeaderboardType.Voice   => "phút",
                _ => ""
            };

            var lines = snap.Entries.OrderBy(e => e.Rank).Take(3).Select(e =>
            {
                var user  = guild.GetUser(e.UserId);
                var name  = user?.DisplayName ?? $"<@{e.UserId}>";
                var delta = e.DeltaRank > 0 ? $"↑{e.DeltaRank}" : e.DeltaRank < 0 ? $"↓{Math.Abs(e.DeltaRank)}" : "─";
                return $"{MedalEmoji(e.Rank)} {name} — **{e.Value:N0}** {unit} `{delta}`";
            });

            embed.AddField($"{icon} Top {snap.Type}", string.Join("\n", lines));
        }

        try
        {
            await guild.SystemChannel.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to post leaderboard announcement for guild {GuildId}", guildId);
        }
    }

    private static string MedalEmoji(int rank) => rank switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $"#{rank}"
    };
}
