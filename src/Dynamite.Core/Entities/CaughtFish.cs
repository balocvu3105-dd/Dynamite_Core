// src/Dynamite.Core/Entities/CaughtFish.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Một con cá (hoặc hải sản) đang ở trong túi của user.
/// Bị xóa khi user bán (/bag sell).
/// </summary>
public class CaughtFish : BaseEntity
{
    public Guid BagId { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    public string FishName  { get; set; } = string.Empty;
    public string FishEmoji { get; set; } = string.Empty;
    public string Rarity    { get; set; } = string.Empty;

    /// <summary>Giá trị coin tại thời điểm câu (dùng khi bán).</summary>
    public long CoinValue { get; set; }

    /// <summary>Null = main pool. Non-null = tên special pool (ví dụ "Vịnh San Hô").</summary>
    public string? SourcePool { get; set; }

    public bool IsSpecialCreature { get; set; } = false;
    public bool IsPearl           { get; set; } = false;

    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserFishBag Bag { get; set; } = null!;
}
