// src/Dynamite.Core/Entities/GuildLevelRole.cs
namespace Dynamite.Core.Entities;

public enum LevelType { Server, Fishing }

/// <summary>
/// Mapping level → Discord role.
/// Admin dùng /levelrole set để cấu hình.
/// XpService tự assign/revoke khi user level up.
/// </summary>
public class GuildLevelRole : BaseEntity
{
    public ulong GuildId { get; set; }
    public LevelType LevelType { get; set; }
    public int RequiredLevel { get; set; }
    public ulong RoleId { get; set; }
}
