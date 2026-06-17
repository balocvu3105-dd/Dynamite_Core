// src/Dynamite.Core/Entities/UserFishBag.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Túi cá per-user per-guild.
/// Default 5 slot, mua "Túi Mở Rộng" để tăng tối đa 50.
/// Khi túi đầy → cá rơi xuống biển (user chỉ nhận coins).
/// </summary>
public class UserFishBag : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    /// <summary>Dung lượng hiện tại (5 → 50). Tăng khi mua BagUpgrade từ shop.</summary>
    public int BagCapacity { get; set; } = 5;

    public ICollection<CaughtFish> Fish { get; set; } = [];

    // Computed helpers (không lưu DB)
    public bool IsFull => Fish.Count >= BagCapacity;
    public int UsedSlots => Fish.Count;
    public int FreeSlots => BagCapacity - Fish.Count;
}
