namespace Dynamite.Core.Entities;

public class Warning : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong TargetUserId { get; set; }
    public ulong ModeratorId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;
}
