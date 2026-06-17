// src/Dynamite.Modules.Economy/Services/ShopService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class ShopService
{
    private readonly IWalletRepository _walletRepo;
    private readonly IShopRepository _shopRepo;
    private readonly FishBagService _bagService;
    private readonly ILogger<ShopService> _logger;

    public ShopService(
        IWalletRepository walletRepo,
        IShopRepository shopRepo,
        FishBagService bagService,
        ILogger<ShopService> logger)
    {
        _walletRepo = walletRepo;
        _shopRepo   = shopRepo;
        _bagService = bagService;
        _logger     = logger;
    }

    public Task<List<InventoryItem>> GetShopItemsAsync(ulong guildId)
        => _shopRepo.GetAvailableItemsAsync(guildId);

    public async Task<(bool success, string message)> BuyAsync(
        ulong guildId, ulong userId, string itemName)
    {
        var item = await _shopRepo.GetItemByNameAsync(guildId, itemName);
        if (item is null)
            return (false, $"Không tìm thấy **{itemName}** trong cửa hàng.");

        if (item.Price <= 0)
            return (false, "Vật phẩm này không thể mua.");

        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);

        if (wallet.Coins < item.Price)
            return (false,
                $"Không đủ coins. Cần **{item.Price:N0}** nhưng bạn chỉ có **{wallet.Coins:N0}**.");

        // ── BagUpgrade — không lưu vào UserInventory, áp dụng thẳng ────────
        if (item.Type == ItemType.BagUpgrade)
        {
            var targetCap = item.UsageCount ?? 20;
            var (bagOk, bagMsg) = await _bagService.UpgradeBagAsync(guildId, userId, targetCap);
            if (!bagOk) return (false, bagMsg);

            wallet.Coins -= item.Price;
            await _walletRepo.AddTransactionAsync(new Transaction
            {
                GuildId      = guildId,
                FromWalletId = wallet.Id,
                Amount       = item.Price,
                Type         = TransactionType.Purchase,
                Note         = $"Mua {item.Name}",
                CreatedAt    = DateTime.UtcNow
            });
            await _walletRepo.SaveChangesAsync();
            return (true, $"✅ {item.Emoji} **{item.Name}** — túi cá nâng lên **{targetCap}** slot!");
        }

        // ── FishingRod — không stack ─────────────────────────────────────────
        var existing = await _shopRepo.GetUserItemAsync(wallet.Id, item.Id);
        if (existing is not null && item.Type == ItemType.FishingRod)
            return (false, $"Bạn đã có **{item.Name}** rồi!");

        wallet.Coins -= item.Price;

        if (existing is not null)
            existing.Quantity++;
        else
            await _shopRepo.AddUserInventoryAsync(new UserInventory
            {
                WalletId   = wallet.Id,
                ItemId     = item.Id,
                Quantity   = 1,
                AcquiredAt = DateTime.UtcNow
            });

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId      = guildId,
            FromWalletId = wallet.Id,
            Amount       = item.Price,
            Type         = TransactionType.Purchase,
            Note         = $"Mua {item.Name}",
            CreatedAt    = DateTime.UtcNow
        });

        await _walletRepo.SaveChangesAsync();
        await _shopRepo.SaveChangesAsync();

        _logger.LogInformation("User {UserId} bought {Item} for {Price} coins", userId, item.Name, item.Price);
        return (true, $"✅ {item.Emoji} **{item.Name}** — **{item.Price:N0}** coins!");
    }

    public async Task<List<UserInventory>> GetInventoryAsync(ulong guildId, ulong userId)
    {
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        return await _shopRepo.GetUserInventoryAsync(wallet.Id);
    }

    /// <summary>
    /// Seed danh sách vật phẩm mặc định vào shop của guild.
    /// Bỏ qua item đã tồn tại (idempotent).
    /// Trả về số lượng item được thêm mới.
    /// </summary>
    public async Task<int> SeedDefaultItemsAsync(ulong guildId)
    {
        var defaults = BuildDefaultItems(guildId);
        int added = 0;

        foreach (var item in defaults)
        {
            var existing = await _shopRepo.GetItemByNameAsync(guildId, item.Name);
            if (existing is not null) continue;

            await _shopRepo.AddItemAsync(item);
            added++;
        }

        if (added > 0)
            await _shopRepo.SaveChangesAsync();

        return added;
    }

    private static List<InventoryItem> BuildDefaultItems(ulong guildId) =>
    [
        // ── Cần Câu ──────────────────────────────────────────────────────────
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Tre",
            Emoji           = "🪁",
            Price           = 1_000,
            Description     = "Cần câu cơ bản. Giảm nhẹ tỉ lệ hụt.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 22,
            DropMultiplier  = 1.0,
            MissRate        = 0.12,
            EscapeRate      = 0.08,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Bạc",
            Emoji           = "🎣",
            Price           = 8_000,
            Description     = "Cần câu nâng cấp. Tăng phần thưởng và giảm cooldown.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 18,
            DropMultiplier  = 1.3,
            MissRate        = 0.10,
            EscapeRate      = 0.07,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Vàng",
            Emoji           = "🏆",
            Price           = 25_000,
            Description     = "Cần câu cao cấp. Phần thưởng tốt hơn đáng kể.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 14,
            DropMultiplier  = 1.6,
            MissRate        = 0.07,
            EscapeRate      = 0.05,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Kim Cương",
            Emoji           = "💎",
            Price           = 70_000,
            Description     = "Cần câu huyền thoại. Hiệu quả vượt trội.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 8,
            DropMultiplier  = 2.5,
            MissRate        = 0.04,
            EscapeRate      = 0.03,
        },

        // ── Mồi Câu ──────────────────────────────────────────────────────────
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Mồi Câu Thường",
            Emoji           = "🪱",
            Price           = 400,
            Description     = "+10% tỉ lệ câu được cá Hiếm. Dùng được 10 lần.",
            Type            = ItemType.Bait,
            IsAvailable     = true,
            UsageCount      = 10,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Mồi Câu Cao Cấp",
            Emoji           = "🦗",
            Price           = 1_200,
            Description     = "+10% tỉ lệ câu được cá Hiếm. Dùng được 30 lần.",
            Type            = ItemType.Bait,
            IsAvailable     = true,
            UsageCount      = 30,
        },

        // ── Nâng Túi Cá ──────────────────────────────────────────────────────
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Túi Cá Mở Rộng",
            Emoji           = "🎒",
            Price           = 5_000,
            Description     = "Nâng sức chứa túi cá lên 30 slot.",
            Type            = ItemType.BagUpgrade,
            IsAvailable     = true,
            UsageCount      = 30,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Túi Cá Siêu To",
            Emoji           = "🧳",
            Price           = 15_000,
            Description     = "Nâng sức chứa túi cá lên 50 slot.",
            Type            = ItemType.BagUpgrade,
            IsAvailable     = true,
            UsageCount      = 50,
        },

        // ── Phép Thời Tiết ────────────────────────────────────────────────────
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Phép Triệu Mưa",
            Emoji           = "🌧️",
            Price           = 2_500,
            Description     = "Kích hoạt thời tiết Rainy trong 60 phút (+Rare chance).",
            Type            = ItemType.WeatherItem,
            IsAvailable     = true,
            DurationMinutes = 60,
        },
    ];

    public async Task<(bool success, string message)> AddShopItemAsync(
        ulong guildId, string name, string emoji, long price,
        string? description, ItemType type,
        int? cooldownSeconds, double? dropMultiplier,
        int? usageCount, int? durationMinutes)
    {
        if (price <= 0)
            return (false, "Giá phải lớn hơn 0.");

        var existing = await _shopRepo.GetItemByNameAsync(guildId, name);
        if (existing is not null)
            return (false, $"**{name}** đã tồn tại trong cửa hàng.");

        await _shopRepo.AddItemAsync(new InventoryItem
        {
            GuildId         = guildId,
            Name            = name,
            Emoji           = emoji,
            Price           = price,
            Description     = description,
            Type            = type,
            IsAvailable     = true,
            CooldownSeconds = cooldownSeconds,
            DropMultiplier  = dropMultiplier,
            UsageCount      = usageCount,
            DurationMinutes = durationMinutes
        });
        await _shopRepo.SaveChangesAsync();

        return (true, $"✅ Đã thêm **{name}** vào cửa hàng.");
    }
}
