// src/Dynamite.Core/Entities/UserInventory.cs
namespace Dynamite.Core.Entities;

public class UserInventory : BaseEntity
{
    public Guid WalletId { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AcquiredAt { get; set; }

    public UserWallet Wallet { get; set; } = null!;
    public InventoryItem Item { get; set; } = null!;
}