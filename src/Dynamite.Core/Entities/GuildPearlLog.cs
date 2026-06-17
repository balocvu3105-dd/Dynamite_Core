// src/Dynamite.Core/Entities/GuildPearlLog.cs
namespace Dynamite.Core.Entities;

public enum PearlType
{
    OceanPearl,  // Viên Ngọc Đại Dương — từ main pool (0.001%)
    SeaEye        // Con Mắt Biển Cả — từ special pool (0.00001%)
}

/// <summary>
/// Log mỗi viên ngọc/mắt biển được câu trong guild.
/// Dùng để enforce giới hạn 3 viên / 7 ngày / guild.
/// </summary>
public class GuildPearlLog : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId  { get; set; }
    public PearlType PearlType { get; set; }
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
