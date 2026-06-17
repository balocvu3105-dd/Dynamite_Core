// src/Dynamite.Core/Entities/GuildConfig.cs
namespace Dynamite.Core.Entities;
public class GuildConfig : BaseEntity
{
    public ulong GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public bool ModerationEnabled { get; set; } = true;
    public bool WelcomeEnabled { get; set; } = false;
    public bool LoggingEnabled { get; set; } = false;
    public bool AutoRoleEnabled { get; set; } = false;
    public ulong? ModLogChannelId { get; set; }
    // Logging channels (Phase 6)
    public ulong? MessageLogChannelId { get; set; }
    public ulong? MemberLogChannelId { get; set; }
    public ulong? VoiceLogChannelId { get; set; }
    public ulong? ServerLogChannelId { get; set; }
    // Audit log — immutable channel for owner/dev only
    public ulong? AuditLogChannelId { get; set; }
    // Welcome + Verify (Phase 7)
    public ulong? WelcomeChannelId { get; set; }
    public string? WelcomeMessage { get; set; }
    // Welcome embed tùy biến — null = dùng mặc định
    public string? WelcomeEmbedTitle { get; set; }   // hỗ trợ {user} {server} {count}
    public string? WelcomeEmbedColor { get; set; }   // hex, vd "#57F287"
    public string? WelcomeEmbedFooter { get; set; }  // hỗ trợ {user} {server} {count}
    public bool WelcomeImageEnabled { get; set; } = true;
    public ulong? VerifyChannelId { get; set; }
    public ulong? VerifyRoleId { get; set; }
    // Role bị THU HỒI sau khi verify thành công (vd: role "khách" tạm). Null = không gỡ.
    public ulong? VerifyRemoveRoleId { get; set; }
    public ICollection<Warning> Warnings { get; set; } = [];
    public ICollection<ModerationAction> ModerationActions { get; set; } = [];
    public ICollection<AutoRoleConfig> AutoRoles { get; set; } = [];
    public ICollection<RolePanel> RolePanels { get; set; } = [];
    // Phase 8
    public AntiSpamConfig? AntiSpamConfig { get; set; }

    // Phase 5 — Temp Voice
    public TempVoiceConfig? TempVoiceConfig { get; set; }

    // Phase Economy v2 — channel riêng cho daily + fishing (lưu trong GuildPond, copy ở đây để query nhanh)
    /// <summary>Channel dùng lệnh /daily. Null = mọi channel đều dùng được.</summary>
    public ulong? DailyChannelId { get; set; }
    /// <summary>Channel câu cá. Null = mọi channel đều dùng được.</summary>
    public ulong? FishingChannelId { get; set; }

    // ── Phase A Channel System ────────────────────────────────────────────────

    // Leaderboard auto-post channels
    /// <summary>Channel post bảng xếp hạng ngư dân (Fishing + Collector) mỗi Chủ Nhật.</summary>
    public ulong? FishingLeaderboardChannelId { get; set; }
    /// <summary>Channel post bảng xếp hạng server (Chat + Voice) mỗi Chủ Nhật.</summary>
    public ulong? ServerLeaderboardChannelId { get; set; }

    // Special Pool announcement
    /// <summary>Channel thông báo khi pool đặc biệt xuất hiện (20:00–05:00 VN).</summary>
    public ulong? SpecialPoolChannelId { get; set; }

    // Shop showcase — embed danh sách item cho toàn server xem
    /// <summary>Channel trưng bày cửa hàng (bot post/edit embed khi item thay đổi).</summary>
    public ulong? ShopChannelId { get; set; }
    /// <summary>Message ID của embed trưng bày cửa hàng (để bot edit thay vì post mới).</summary>
    public ulong? ShopShowcaseMessageId { get; set; }

    // Invoice channel — receipt giao dịch
    /// <summary>Channel nhận hóa đơn mua bán. Null = không gửi hóa đơn public.</summary>
    public ulong? InvoiceChannelId { get; set; }

    // Weather forecast
    /// <summary>Channel dự báo thời tiết bể cá (bot edit embed khi thời tiết đổi).</summary>
    public ulong? WeatherChannelId { get; set; }
    /// <summary>Message ID của embed dự báo thời tiết.</summary>
    public ulong? WeatherForecastMessageId { get; set; }

    // Guide / Cẩm nang
    /// <summary>Channel cẩm nang hướng dẫn (bot post các embed preset).</summary>
    public ulong? GuideChannelId { get; set; }
}
