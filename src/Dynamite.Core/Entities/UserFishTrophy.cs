// src/Dynamite.Core/Entities/UserFishTrophy.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Lưu danh sách loài cá Rare+ đã từng câu được (mỗi loài chỉ ghi 1 lần).
/// Dùng cho Collector leaderboard — ai sưu tầm được nhiều loài nhất.
///
/// Unique constraint: (GuildId, UserId, FishName) — tránh duplicate.
/// Không bị prune như FishingActivityLog.
/// </summary>
public class UserFishTrophy : BaseEntity
{
    public ulong GuildId  { get; set; }
    public ulong UserId   { get; set; }
    public string FishName { get; set; } = string.Empty;
    public string Rarity   { get; set; } = string.Empty;   // Rare / Legendary / Mythic
    public bool   IsPearl  { get; set; }
    public bool   IsSpecial { get; set; }
}
