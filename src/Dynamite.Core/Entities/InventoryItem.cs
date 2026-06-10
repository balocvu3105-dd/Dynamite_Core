// src/Dynamite.Core/Entities/InventoryItem.cs
namespace Dynamite.Core.Entities;

public enum ItemType
{
    Collectible,
    FishingRod,
    Consumable
}

public class InventoryItem : BaseEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Emoji { get; set; } = "📦";
    public long Price { get; set; }
    public ItemType Type { get; set; } = ItemType.Collectible;
    public bool IsAvailable { get; set; } = true;

    // Chỉ dùng khi Type == FishingRod
    public int? CooldownSeconds { get; set; }
    public double? DropMultiplier { get; set; }

    public ICollection<UserInventory> Owners { get; set; } = [];
}