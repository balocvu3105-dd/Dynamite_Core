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

    // Welcome + Verify (Phase 7)
    public ulong? WelcomeChannelId { get; set; }
    public string? WelcomeMessage { get; set; }
    public ulong? VerifyChannelId { get; set; }
    public ulong? VerifyRoleId { get; set; }

    public ICollection<Warning> Warnings { get; set; } = [];
    public ICollection<ModerationAction> ModerationActions { get; set; } = [];
    public ICollection<AutoRoleConfig> AutoRoles { get; set; } = [];
    public ICollection<RolePanel> RolePanels { get; set; } = [];

    // Phase 8
    public AntiSpamConfig? AntiSpamConfig { get; set; }
}