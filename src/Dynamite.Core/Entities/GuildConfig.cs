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
    public ulong? ServerLogChannelId { get; set; }

    public ICollection<Warning> Warnings { get; set; } = [];
    public ICollection<ModerationAction> ModerationActions { get; set; } = [];

    // Phase 3 additions
    public ICollection<AutoRoleConfig> AutoRoles { get; set; } = [];
    public ICollection<RolePanel> RolePanels { get; set; } = [];
}