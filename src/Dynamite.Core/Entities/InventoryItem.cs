// src/Dynamite.Core/Entities/InventoryItem.cs
namespace Dynamite.Core.Entities;

public enum ItemType
{
    Collectible,
    FishingRod,
    Consumable,
    Bait,        // Lucky Bait: +10% Rare trong N lần câu tiếp theo
    AutoFish,    // Gói auto-câu X phút (Y lần, 10s/lần)
    WeatherItem, // Rain Charm: force weather Rainy N phút
    BagUpgrade,  // Mở rộng túi cá: UsageCount = dung lượng mới (20 hoặc 50)
    PoolTicket   // Vé vào pool đặc biệt: 1 vé = 1 lần câu trong Special Pool
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

    // FishingRod
    public int? CooldownSeconds { get; set; }
    public double? DropMultiplier { get; set; }
    /// <summary>Tỉ lệ hụt trước khi cá cắn (0.0–1.0). Default 0.15 nếu null.</summary>
    public double? MissRate { get; set; }
    /// <summary>Tỉ lệ cá cắn rồi thoát sau khi roll (0.0–1.0). Default 0.10 nếu null.</summary>
    public double? EscapeRate { get; set; }

    // Bait / AutoFish / WeatherItem — số lần hoặc số phút hiệu lực
    public int? UsageCount { get; set; }
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Độ bền tối đa khi mua mới (chỉ FishingRod). null = không track durability.
    /// Giảm 1 mỗi lần câu thành công; về 0 → cần bị "gãy", cần repair.
    /// </summary>
    public int? MaxDurability { get; set; }

    public ICollection<UserInventory> Owners { get; set; } = [];
}