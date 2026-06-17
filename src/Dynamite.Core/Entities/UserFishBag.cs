// src/Dynamite.Core/Entities/UserFishBag.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Túi cá per-user per-guild.
/// Default 10 slot. Mua "Nâng Túi Cá +10" để tăng dần, tối đa 100 slot.
/// Khi túi đầy → auto-fish tạm dừng, câu thủ công nhận coins thay vì lưu cá.
/// </summary>
public class UserFishBag : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    /// <summary>Dung lượng hiện tại (10 → 100). Tăng khi mua BagUpgrade từ shop.</summary>
    public int BagCapacity { get; set; } = 10;

    public ICollection<CaughtFish> Fish { get; set; } = [];

    // Computed helpers (không lưu DB)
    public bool IsFull => Fish.Count >= BagCapacity;
    public int UsedSlots => Fish.Count;
    public int FreeSlots => BagCapacity - Fish.Count;
}
