// src/Dynamite.Core/Entities/TempVoiceConfig.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Per-guild config cho Temp Voice system.
/// Lưu channel nào là "trigger" — user join vào đó thì bot tạo room riêng cho họ.
/// </summary>
public class TempVoiceConfig : BaseEntity
{
    public ulong GuildId { get; set; }

    /// <summary>Channel mà user phải join để trigger tạo room.</summary>
    public ulong TriggerChannelId { get; set; }

    /// <summary>
    /// Category để tạo temp channel trong đó.
    /// Null = tạo cùng category với trigger channel.
    /// </summary>
    public ulong? CategoryId { get; set; }

    /// <summary>User limit mặc định cho mỗi temp room. 0 = unlimited.</summary>
    public int DefaultUserLimit { get; set; } = 0;

    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;
}
