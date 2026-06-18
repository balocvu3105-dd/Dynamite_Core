// src/Dynamite.Core/Entities/UserInventory.cs
namespace Dynamite.Core.Entities;

public class UserInventory : BaseEntity
{
    public Guid WalletId { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AcquiredAt { get; set; }

    /// <summary>
    /// Độ bền hiện tại (chỉ FishingRod có MaxDurability). null = không track (legacy hoặc non-rod).
    /// 0 = cần câu gãy, không dùng được cho đến khi repair.
    /// </summary>
    public int? RodDurability { get; set; }

    public UserWallet Wallet { get; set; } = null!;
    public InventoryItem Item { get; set; } = null!;
}