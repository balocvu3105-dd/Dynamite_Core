namespace Dynamite.Core.Entities;

public class Warning : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong TargetUserId { get; set; }
    public string TargetUsername { get; set; } = string.Empty;
    public ulong ModeratorId { get; set; }
    public string ModeratorUsername { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;
}
