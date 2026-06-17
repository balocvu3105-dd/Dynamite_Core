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

    // Phase Economy v2 — channel riêng cho daily + fishing
    public ulong? DailyChannelId { get; set; }
    public ulong? FishingChannelId { get; set; }
}