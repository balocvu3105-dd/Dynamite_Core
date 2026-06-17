// src/Dynamite.Core/Entities/UserFishingProfile.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Hồ sơ câu cá per-user per-guild:
/// fishing XP pool riêng, cooldown DB-backed, stats cho achievements.
/// </summary>
public class UserFishingProfile : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // ── XP + Level ──────────────────────────────────────────────────────────
    public long FishingXp { get; set; } = 0;
    public int FishingLevel { get; set; } = 0;

    // ── Cooldown (DB-backed, không dùng MemoryCache) ────────────────────────
    public DateTime? LastFishedAt { get; set; }

    // ── Catch stats (dùng cho achievements) ─────────────────────────────────
    public int TotalCaught { get; set; } = 0;
    public int CommonCaught { get; set; } = 0;
    public int UncommonCaught { get; set; } = 0;
    public int RareCaught { get; set; } = 0;
    public int LegendaryCaught { get; set; } = 0;
    public int MythicCaught { get; set; } = 0;
    public int ChestsOpened { get; set; } = 0;

    // ── Auto-fish session ────────────────────────────────────────────────────
    /// <summary>Thời điểm hết hạn auto-fish. Null = không có session.</summary>
    public DateTime? AutoFishExpiresAt { get; set; }

    /// <summary>
    /// true  = user thường mua gói → bán tất cả sau mỗi lần câu.
    /// false = admin/owner session → giữ Rare+, chỉ bán Common/Uncommon.
    /// </summary>
    public bool AutoFishSellAll { get; set; } = false;

    /// <summary>
    /// Số lần user đã mua gói auto-fish (dùng để tính escalating price).
    /// Admin không tính vào đây.
    /// </summary>
    public int AutoFishPurchaseCount { get; set; } = 0;

    /// <summary>
    /// Channel ID để post kết quả auto-fish (user mode).
    /// 0 = chưa set. Admin mode không dùng field này.
    /// </summary>
    public ulong AutoFishChannelId { get; set; } = 0;

    /// <summary>
    /// Tạm dừng session — bot không câu nhưng timer vẫn chạy.
    /// User dùng /fish-auto pause để bật, /fish-auto resume để tắt.
    /// </summary>
    public bool AutoFishPaused { get; set; } = false;

    // ── Trade throttle ───────────────────────────────────────────────────────
    public int TradesThisWeek { get; set; } = 0;
    public DateTime? TradeWeekResetAt { get; set; }

    public ICollection<UserFishingAchievement> Achievements { get; set; } = [];
}
