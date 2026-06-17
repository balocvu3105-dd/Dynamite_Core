// src/Dynamite.Modules.Economy/Services/SpecialPoolScheduler.cs
namespace Dynamite.Modules.Economy.Services;

using System.Globalization;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// IHostedService chạy nền — spawn Special Pool theo lịch xen kẽ tuần:
///
/// LỊCH MỞ:
///   • Giờ mở  : 20:00 Vietnam (13:00 UTC) — kiểm tra mỗi 5 phút
///   • Giờ đóng: 05:00 Vietnam hôm sau (22:00 UTC = mở 9 tiếng)
///   • Điều kiện mở ngày:
///     - Tuần chẵn (ISO week number chẵn) → mở các ngày CHẴN trong tháng
///     - Tuần lẻ  (ISO week number lẻ)   → mở các ngày LẺ  trong tháng
///     → Tuần này chẵn → tuần sau lẻ → lặp lại
///
/// ĐIỀU KIỆN VÀO:
///   • Fishing Level 20+
///   • Sở hữu Vé Pool Đặc Biệt (1 vé / 1 lần)
///
/// THÔNG BÁO: SpecialPoolChannelId → FishingChannelId → SystemChannel
/// </summary>
public class SpecialPoolScheduler : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    /// <summary>Vietnam Standard Time (UTC+7). Works on both Windows and Linux.</summary>
    private static readonly TimeZoneInfo VnTz = GetVnTimeZone();

    private static readonly (string Name, SpecialDropTable Table)[] PoolTypes =
    [
        ("Vịnh San Hô 🪸",        SpecialDropTable.CoralBay),
        ("Đáy Đại Dương 🌊",       SpecialDropTable.DeepOcean),
        ("Rừng Ngập Mặn 🌿",       SpecialDropTable.MangroveForest),
        ("Vực Thẳm Huyền Bí 🌑",  SpecialDropTable.AbyssalZone),
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient  _discord;
    private readonly ILogger<SpecialPoolScheduler> _logger;

    public SpecialPoolScheduler(
        IServiceScopeFactory scopeFactory,
        DiscordSocketClient  discord,
        ILogger<SpecialPoolScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _discord      = discord;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SpecialPoolScheduler started (VN tz = {Tz})", VnTz.DisplayName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSpawnAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SpecialPoolScheduler.CheckAndSpawnAsync");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    // ── Core spawn check ──────────────────────────────────────────────────────

    private async Task CheckAndSpawnAsync()
    {
        var utcNow = DateTime.UtcNow;
        var vnNow  = TimeZoneInfo.ConvertTimeFromUtc(utcNow, VnTz);

        // Chỉ spawn trong cửa sổ 20:00–20:05 Vietnam time
        if (vnNow.Hour != 20 || vnNow.Minute >= 5)
            return;

        // Kiểm tra lịch xen kẽ tuần
        if (!ShouldSpawnToday(vnNow))
        {
            var weekNum = ISOWeek.GetWeekOfYear(vnNow);
            _logger.LogDebug(
                "SpecialPool: no pool today (week {Week} = {WeekParity}, day {Day} = {DayParity})",
                weekNum,
                weekNum % 2 == 0 ? "chẵn" : "lẻ",
                vnNow.Day,
                vnNow.Day % 2 == 0 ? "chẵn" : "lẻ");
            return;
        }

        using var scope   = _scopeFactory.CreateScope();
        var poolRepo      = scope.ServiceProvider.GetRequiredService<ISpecialPoolRepository>();
        var pondRepo      = scope.ServiceProvider.GetRequiredService<IPondRepository>();
        var configRepo    = scope.ServiceProvider.GetRequiredService<IGuildConfigRepository>();

        var allPonds = await pondRepo.GetAllAsync();

        foreach (var pond in allPonds)
        {
            try
            {
                await SpawnForGuildAsync(pond.GuildId, utcNow, vnNow, poolRepo, configRepo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to spawn pool for guild {GuildId}", pond.GuildId);
            }
        }
    }

    private async Task SpawnForGuildAsync(
        ulong guildId,
        DateTime utcNow,
        DateTime vnNow,
        ISpecialPoolRepository poolRepo,
        IGuildConfigRepository configRepo)
    {
        // Idempotency: không spawn 2 lần trong cùng ngày UTC
        var existingToday = await poolRepo.GetTodayPoolCountAsync(guildId, utcNow);
        if (existingToday > 0)
        {
            _logger.LogDebug("Guild {GuildId}: pool already spawned today", guildId);
            return;
        }

        // Tính ExpiresAt = 05:00 Vietnam sáng hôm sau → UTC
        var vnExpiry  = new DateTime(vnNow.Year, vnNow.Month, vnNow.Day, 5, 0, 0).AddDays(1);
        var utcExpiry = TimeZoneInfo.ConvertTimeToUtc(vnExpiry, VnTz); // = utcNow + 9 giờ

        // Chọn ngẫu nhiên 1 loại pool
        var (name, table) = PoolTypes[Random.Shared.Next(PoolTypes.Length)];

        var pool = new SpecialPool
        {
            GuildId       = guildId,
            PoolName      = name,
            DropTable     = table,
            Capacity      = 2000,
            RemainingFish = 2000,
            MinLevel      = 20,
            StartsAt      = utcNow,
            ExpiresAt     = utcExpiry,
            CreatedAt     = utcNow
        };

        await poolRepo.AddPoolAsync(pool);
        await poolRepo.SaveChangesAsync();

        _logger.LogInformation(
            "Spawned special pool [{Name}] for guild {GuildId} — expires at {Expiry} UTC (05:00 VN)",
            name, guildId, utcExpiry.ToString("HH:mm dd/MM"));

        await AnnouncePoolAsync(guildId, pool, vnExpiry, configRepo);
    }

    // ── Schedule logic ────────────────────────────────────────────────────────

    /// <summary>
    /// Tuần chẵn (ISO week number chẵn) → chỉ mở ngày CHẴN trong tháng.
    /// Tuần lẻ  (ISO week number lẻ)   → chỉ mở ngày LẺ  trong tháng.
    /// Kết quả: tuần này mở ngày chẵn → tuần sau mở ngày lẻ → xen kẽ tự động.
    /// </summary>
    private static bool ShouldSpawnToday(DateTime vnNow)
    {
        var weekNumber = ISOWeek.GetWeekOfYear(vnNow);
        var isEvenWeek = weekNumber % 2 == 0;
        var isEvenDay  = vnNow.Day  % 2 == 0;

        // Tuần chẵn ↔ ngày chẵn, tuần lẻ ↔ ngày lẻ
        return isEvenWeek == isEvenDay;
    }

    // ── Discord announcement ──────────────────────────────────────────────────

    private async Task AnnouncePoolAsync(
        ulong guildId,
        SpecialPool pool,
        DateTime vnExpiry,
        IGuildConfigRepository configRepo)
    {
        var guild = _discord.GetGuild(guildId);
        if (guild is null) return;

        // Ưu tiên: SpecialPoolChannelId → FishingChannelId → SystemChannel
        var config = await configRepo.GetByGuildIdAsync(guildId);

        ITextChannel? channel = null;
        if (config?.SpecialPoolChannelId.HasValue == true)
            channel = guild.GetTextChannel(config.SpecialPoolChannelId.Value);
        if (channel is null && config?.FishingChannelId.HasValue == true)
            channel = guild.GetTextChannel(config.FishingChannelId.Value);
        if (channel is null)
            channel = guild.SystemChannel;

        if (channel is null)
        {
            _logger.LogWarning(
                "No channel to announce pool for guild {GuildId} — set /pool set-channel", guildId);
            return;
        }

        var expiryUnix  = new DateTimeOffset(vnExpiry, TimeSpan.FromHours(7)).ToUnixTimeSeconds();
        var weekNum     = ISOWeek.GetWeekOfYear(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTz));
        var weekParity  = weekNum % 2 == 0 ? "Chẵn" : "Lẻ";
        var dayParity   = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTz).Day % 2 == 0
                          ? "Chẵn" : "Lẻ";

        var embed = new EmbedBuilder()
            .WithTitle("🌊 Pool Đặc Biệt Xuất Hiện!")
            .WithDescription(
                $"**{pool.PoolName}** vừa mở cửa!\n\n" +
                $"🕗 Thời gian: **20:00 → 05:00** (giờ Việt Nam)\n" +
                $"⏰ Đóng lúc: <t:{expiryUnix}:F>\n" +
                $"🐟 Cá còn lại: **{pool.RemainingFish:N0}**")
            .WithColor(new Color(0x1F8EF1))
            .WithFooter(
                $"Yêu cầu: Level 20+ và Vé Pool Đặc Biệt 🎟️  •  Tuần {weekNum} ({weekParity}) • Ngày {dayParity}")
            .AddField("Cách tham gia", "`/fishing pool cast` → chọn pool vừa xuất hiện")
            .WithCurrentTimestamp()
            .Build();

        try
        {
            await channel.SendMessageAsync(embed: embed);
            _logger.LogInformation(
                "[SpecialPool] Announced pool [{Name}] to #{Channel} in guild {GuildId}",
                pool.PoolName, channel.Name, guildId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to announce special pool for guild {GuildId}", guildId);
        }
    }

    // ── Timezone helper ───────────────────────────────────────────────────────

    private static TimeZoneInfo GetVnTimeZone()
    {
        // Linux (Docker): "Asia/Ho_Chi_Minh" | Windows: "SE Asia Standard Time"
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* try next */ }
        }

        // Fallback: UTC+7 fixed
        return TimeZoneInfo.CreateCustomTimeZone(
            "VN-Fallback", TimeSpan.FromHours(7), "Vietnam Standard Time", "Vietnam Standard Time");
    }
}
