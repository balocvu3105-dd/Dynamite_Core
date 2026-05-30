namespace Dynamite.Core.Entities;

using Dynamite.Core.Enums;

public class ModerationAction : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong TargetUserId { get; set; }
    public ulong ModeratorId { get; set; }
    public ModerationActionType ActionType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }

    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;
}
