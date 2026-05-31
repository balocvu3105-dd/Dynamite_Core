namespace Dynamite.Core.Entities;

public class AutoRoleConfig : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }

    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;
}