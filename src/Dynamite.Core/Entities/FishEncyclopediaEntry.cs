// src/Dynamite.Core/Entities/FishEncyclopediaEntry.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Ghi nhận mỗi loại cá mà user đã từng câu được trong guild.
/// Unique index: (GuildId, UserId, FishName).
///
/// Dùng cho /fishing dex — hiển thị "encyclopedia" của người chơi.
/// Không lưu lịch sử từng lần câu — chỉ lưu tổng hợp (upsert mỗi lần catch).
/// </summary>
public class FishEncyclopediaEntry : BaseEntity
{
    public ulong    GuildId      { get; set; }
    public ulong    UserId       { get; set; }

    /// <summary>Tên cá/rác/hòm — từ FishCatch.Name hoặc SpecialFishCatch.Name.</summary>
    public string   FishName     { get; set; } = "";

    /// <summary>Emoji đại diện (từ FishCatch.Emoji).</summary>
    public string   Emoji        { get; set; } = "🐟";

    /// <summary>Rarity: Common, Uncommon, Rare, Legendary, Mythic, Trash, Bronze, Gold, Diamond.</summary>
    public string   Rarity       { get; set; } = "Common";

    /// <summary>Số lần câu được loại này.</summary>
    public int      TimesCaught  { get; set; } = 1;

    /// <summary>Giá xu cao nhất từng bán được cho loại này.</summary>
    public long     BestCoins    { get; set; } = 0;

    /// <summary>Lần đầu tiên câu được.</summary>
    public DateTime FirstCaughtAt { get; set; } = DateTime.UtcNow;

    /// <summary>Lần gần nhất câu được.</summary>
    public DateTime LastCaughtAt  { get; set; } = DateTime.UtcNow;
}
