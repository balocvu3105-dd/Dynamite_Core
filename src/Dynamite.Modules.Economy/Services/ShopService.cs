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
    private readonly WeatherService _weatherService;
    private readonly ILogger<ShopService> _logger;

    public ShopService(
        IWalletRepository walletRepo,
        IShopRepository shopRepo,
        FishBagService bagService,
        WeatherService weatherService,
        ILogger<ShopService> logger)
    {
        _walletRepo     = walletRepo;
        _shopRepo       = shopRepo;
        _bagService     = bagService;
        _weatherService = weatherService;
        _logger         = logger;
    }

    // ── Rod repair pricing ────────────────────────────────────────────────────

    /// <summary>
    /// Tính chi phí repair cần câu.
    /// Công thức: (maxDur - currentDur) * (rodPrice * 0.5 / maxDur)
    /// = 50% giá mua gốc nếu repair từ 0 lên max.
    /// </summary>
    public static long GetRepairCost(long rodPrice, int maxDurability, int currentDurability)
    {
        var missing     = maxDurability - currentDurability;
        if (missing <= 0) return 0;
        var costPerUnit = rodPrice * 0.5 / maxDurability;
        return (long)Math.Ceiling(costPerUnit * missing);
    }

    // ── Bag upgrade pricing ───────────────────────────────────────────────────

    private const int DefaultBagCapacity = 10;
    private const int BagSlotStep        = 10;
    private const int MaxBagCap          = 100;

    /// <summary>
    /// Giá nâng +10 slot tại mỗi tier (index = (currentCap - 10) / 10).
    /// Tier 0: 10→20 =  10,000 xu
    /// Tier 1: 20→30 =  20,000 xu
    /// Tier 2: 30→40 =  35,000 xu
    /// Tier 3: 40→50 =  55,000 xu
    /// Tier 4: 50→60 =  80,000 xu
    /// Tier 5: 60→70 = 110,000 xu
    /// Tier 6: 70→80 = 145,000 xu
    /// Tier 7: 80→90 = 185,000 xu
    /// Tier 8: 90→100 = 230,000 xu
    /// </summary>
    private static readonly long[] BagPriceTiers =
        [10_000, 20_000, 35_000, 55_000, 80_000, 110_000, 145_000, 185_000, 230_000];

    public static long GetBagUpgradePrice(int currentCapacity)
    {
        if (currentCapacity >= MaxBagCap) return 0; // đã max
        var tier = (currentCapacity - DefaultBagCapacity) / BagSlotStep;
        return tier >= 0 && tier < BagPriceTiers.Length
            ? BagPriceTiers[tier]
            : BagPriceTiers[^1];
    }

    // ─────────────────────────────────────────────────────────────────────────

    public Task<List<InventoryItem>> GetShopItemsAsync(ulong guildId)
        => _shopRepo.GetAvailableItemsAsync(guildId);

    public async Task<(bool success, string message)> BuyAsync(
        ulong guildId, ulong userId, string itemName)
    {
        var item = await _shopRepo.GetItemByNameAsync(guildId, itemName);
        if (item is null)
            return (false, $"Không tìm thấy **{itemName}** trong cửa hàng.");

        // BagUpgrade dùng dynamic pricing riêng — bỏ qua check price ở đây
        if (item.Price <= 0 && item.Type != ItemType.BagUpgrade)
            return (false, "Vật phẩm này không thể mua.");

        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);

        // Wallet check chung chỉ áp dụng cho non-BagUpgrade (BagUpgrade tự check ở dưới)
        if (item.Type != ItemType.BagUpgrade && wallet.Coins < item.Price)
            return (false,
                $"Không đủ coins. Cần **{item.Price:N0}** nhưng bạn chỉ có **{wallet.Coins:N0}**.");

        // ── BagUpgrade — dynamic pricing theo tier, +10 slot mỗi lần ────────
        if (item.Type == ItemType.BagUpgrade)
        {
            var bag          = await _bagService.GetBagAsync(guildId, userId);
            var dynamicPrice = GetBagUpgradePrice(bag.BagCapacity);

            if (dynamicPrice == 0)
                return (false, $"🎒 Túi cá của bạn đã đạt tối đa **{MaxBagCap}** slot!");

            if (wallet.Coins < dynamicPrice)
                return (false,
                    $"Không đủ coins. Nâng +10 slot hiện tại cần **{dynamicPrice:N0}** nhưng bạn chỉ có **{wallet.Coins:N0}**.");

            var slotsToAdd = item.UsageCount ?? BagSlotStep;
            var (bagOk, _, oldCap, newCap) = await _bagService.AddSlotsAsync(guildId, userId, slotsToAdd);
            if (!bagOk) return (false, $"🎒 Túi cá đã đạt tối đa **{MaxBagCap}** slot!");

            wallet.Coins -= dynamicPrice;
            await _walletRepo.AddTransactionAsync(new Transaction
            {
                GuildId      = guildId,
                FromWalletId = wallet.Id,
                Amount       = dynamicPrice,
                Type         = TransactionType.Purchase,
                Note         = $"Nâng túi cá {oldCap} → {newCap} slot",
                CreatedAt    = DateTime.UtcNow
            });
            await _walletRepo.SaveChangesAsync();
            return (true, $"✅ {item.Emoji} Túi cá **{oldCap} → {newCap}** slot — **{dynamicPrice:N0}** coins!");
        }

        // ── FishingRod — không stack ─────────────────────────────────────────
        var existing = await _shopRepo.GetUserItemAsync(wallet.Id, item.Id);
        if (existing is not null && item.Type == ItemType.FishingRod)
        {
            // Nếu rod đang bị gãy → cho phép "mua lại" = repair về max
            if (existing.RodDurability.HasValue && existing.RodDurability == 0 && item.MaxDurability.HasValue)
            {
                var repairCost = GetRepairCost(item.Price, item.MaxDurability.Value, 0);
                if (wallet.Coins < repairCost)
                    return (false,
                        $"Cần câu của bạn đang **gãy**! Repair cần **{repairCost:N0}** xu nhưng bạn chỉ có **{wallet.Coins:N0}** xu.\n" +
                        $"Dùng `/shop repair-rod` để sửa.");
                wallet.Coins -= repairCost;
                existing.RodDurability = item.MaxDurability.Value;
                await _walletRepo.AddTransactionAsync(new Transaction
                {
                    GuildId      = guildId,
                    FromWalletId = wallet.Id,
                    Amount       = repairCost,
                    Type         = TransactionType.Purchase,
                    Note         = $"Repair {item.Name}",
                    CreatedAt    = DateTime.UtcNow
                });
                await _walletRepo.SaveChangesAsync();
                return (true, $"🔧 **{item.Name}** đã được sửa chữa! Độ bền: **{item.MaxDurability}** lần câu.");
            }
            return (false, $"Bạn đã có **{item.Name}** rồi!");
        }

        wallet.Coins -= item.Price;

        // Bait/PoolTicket: mỗi lần mua = UsageCount lần dùng (không phải 1).
        // FishingRod: luôn 1 (không stack).
        // Các loại khác: stack 1 đơn vị mỗi lần mua.
        var addQuantity = item.Type == ItemType.Bait || item.Type == ItemType.PoolTicket
            ? item.UsageCount ?? 1
            : 1;

        if (existing is not null)
            existing.Quantity += addQuantity;
        else
            await _shopRepo.AddUserInventoryAsync(new UserInventory
            {
                WalletId      = wallet.Id,
                ItemId        = item.Id,
                Quantity      = addQuantity,
                AcquiredAt    = DateTime.UtcNow,
                // Rod: set durability = MaxDurability khi mua mới
                RodDurability = item.Type == ItemType.FishingRod ? item.MaxDurability : null,
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

    /// <summary>
    /// Mua item và trả về thêm thông tin cho InvoiceService.
    /// Signature: (success, message, item, coinsPaid, coinsRemaining)
    /// </summary>
    public async Task<(bool success, string message, InventoryItem? item, long coinsPaid, long coinsRemaining)>
        BuyWithDetailsAsync(ulong guildId, ulong userId, string itemName)
    {
        var item = await _shopRepo.GetItemByNameAsync(guildId, itemName);
        if (item is null)
            return (false, $"Không tìm thấy **{itemName}** trong cửa hàng.", null, 0, 0);

        if (item.Price <= 0 && item.Type != ItemType.BagUpgrade)
            return (false, "Vật phẩm này không thể mua.", null, 0, 0);

        // Snapshot coins trước khi mua để tính coinsPaid chính xác
        // (BagUpgrade dùng dynamicPrice, không phải item.Price)
        var walletBefore = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var coinsBefore  = walletBefore.Coins;

        if (item.Type != ItemType.BagUpgrade && walletBefore.Coins < item.Price)
            return (false,
                $"Không đủ coins. Cần **{item.Price:N0}** nhưng bạn chỉ có **{walletBefore.Coins:N0}**.",
                null, 0, 0);

        // Delegate to core BuyAsync logic, then compute remaining
        var (success, message) = await BuyAsync(guildId, userId, itemName);
        if (!success) return (false, message, null, 0, 0);

        // coinsPaid = chênh lệch thực tế (đúng với cả BagUpgrade dynamic price)
        var updatedWallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var coinsPaid     = coinsBefore - updatedWallet.Coins;
        return (true, message, item, coinsPaid, updatedWallet.Coins);
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
    /// <summary>
    /// Seed items mặc định: thêm mới nếu chưa tồn tại, update stats/giá nếu đã có.
    /// Trả về (added, updated).
    /// </summary>
    public async Task<(int added, int updated)> SeedDefaultItemsAsync(ulong guildId)
    {
        var defaults = BuildDefaultItems(guildId);
        int added = 0, updated = 0;

        foreach (var item in defaults)
        {
            var existing = await _shopRepo.GetItemByNameAsync(guildId, item.Name);

            if (existing is null)
            {
                await _shopRepo.AddItemAsync(item);
                added++;
            }
            else
            {
                // Sync tất cả stats từ defaults → DB (giá, cooldown, rates, v.v.)
                existing.Emoji           = item.Emoji;
                existing.Price           = item.Price;
                existing.Description     = item.Description;
                existing.IsAvailable     = item.IsAvailable;
                existing.CooldownSeconds = item.CooldownSeconds;
                existing.DropMultiplier  = item.DropMultiplier;
                existing.MissRate        = item.MissRate;
                existing.EscapeRate      = item.EscapeRate;
                existing.UsageCount      = item.UsageCount;
                existing.DurationMinutes = item.DurationMinutes;
                existing.MaxDurability   = item.MaxDurability;
                updated++;
            }
        }

        if (added > 0 || updated > 0)
            await _shopRepo.SaveChangesAsync();

        return (added, updated);
    }

    private static List<InventoryItem> BuildDefaultItems(ulong guildId) =>
    [
        // ── Cần Câu ──────────────────────────────────────────────────────────
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Tre",
            Emoji           = "🪁",
            Price           = 3_000,
            Description     = "Cần câu cơ bản. Giảm nhẹ tỉ lệ hụt, tăng nhẹ giá trị cá. Độ bền 150 lần câu.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 22,
            DropMultiplier  = 1.1,
            MissRate        = 0.13,
            EscapeRate      = 0.09,
            MaxDurability   = 150,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Bạc",
            Emoji           = "🎣",
            Price           = 20_000,
            Description     = "Cần câu nâng cấp. Tăng giá trị cá và giảm tỉ lệ hụt đáng kể. Độ bền 250 lần câu.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 19,
            DropMultiplier  = 1.25,
            MissRate        = 0.11,
            EscapeRate      = 0.08,
            MaxDurability   = 250,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Vàng",
            Emoji           = "🏆",
            Price           = 60_000,
            Description     = "Cần câu cao cấp. Hiệu quả rõ rệt — đầu tư cho người chơi nghiêm túc. Độ bền 400 lần câu.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 15,
            DropMultiplier  = 1.55,
            MissRate        = 0.08,
            EscapeRate      = 0.06,
            MaxDurability   = 400,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Kim Cương",
            Emoji           = "💎",
            Price           = 160_000,
            Description     = "Cần câu huyền thoại. Mục tiêu end-game — sản lượng và giá trị vượt trội. Độ bền 600 lần câu.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 10,
            DropMultiplier  = 2.0,
            MissRate        = 0.05,
            EscapeRate      = 0.04,
            MaxDurability   = 600,
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

        // ── Nâng Túi Cá (+10 slot mỗi lần, giá tăng dần theo tier) ──────────
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Nâng Túi Cá +10",
            Emoji           = "🎒",
            Price           = 1,   // placeholder — giá thực tính động theo BagPriceTiers
            Description     = "Mở rộng túi cá thêm +10 slot. Túi mặc định 10 slot, nâng dần đến tối đa 100 slot. Giá tăng theo tier: 10k → 20k → 35k → 55k → 80k → 110k → 145k → 185k → 230k.",
            Type            = ItemType.BagUpgrade,
            IsAvailable     = true,
            UsageCount      = 10,
        },

        // ── Phép Thời Tiết ────────────────────────────────────────────────────
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Phép Triệu Mưa",
            Emoji           = "🌧️",
            Price           = 20_000,
            Description     = "Kích hoạt thời tiết Rainy trong 60 phút (+Rare chance, sản lượng cao hơn).",
            Type            = ItemType.WeatherItem,
            IsAvailable     = true,
            DurationMinutes = 60,
        },

        // ── Vé Pool Đặc Biệt ─────────────────────────────────────────────────
        new InventoryItem
        {
            GuildId     = guildId,
            Name        = "Vé Pool Đặc Biệt",
            Emoji       = "🎟️",
            Price       = 15_000,
            Description = "Cho phép câu 1 lần tại Special Pool (Level 20+ yêu cầu). Dùng 1 vé/lần câu.",
            Type        = ItemType.PoolTicket,
            IsAvailable = true,
            UsageCount  = 1,
        },
    ];

    /// <summary>
    /// Sử dụng vật phẩm tiêu thụ từ túi đồ.
    /// Trả về (success, message, coinsPaid=0, item) cho InvoiceService.
    /// </summary>
    public async Task<(bool success, string message, InventoryItem? item)> UseItemAsync(
        ulong guildId, ulong userId, string itemName)
    {
        var wallet    = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var inventory = await _shopRepo.GetUserInventoryAsync(wallet.Id);

        var userItem = inventory.FirstOrDefault(i =>
            string.Equals(i.Item.Name, itemName, StringComparison.OrdinalIgnoreCase)
            && i.Quantity > 0);

        if (userItem is null)
            return (false, $"❌ Bạn không có **{itemName}** trong túi đồ.", null);

        var item = userItem.Item;

        switch (item.Type)
        {
            case ItemType.WeatherItem:
            {
                var duration = item.DurationMinutes ?? 60;
                await _weatherService.ForceWeatherAsync(guildId, PondWeather.Rainy, duration);

                userItem.Quantity--;
                if (userItem.Quantity <= 0)
                    await _shopRepo.RemoveUserInventoryAsync(userItem);
                await _shopRepo.SaveChangesAsync();

                _logger.LogInformation(
                    "User {UserId} used [{Item}] → Rainy {Min}min in guild {GuildId}",
                    userId, item.Name, duration, guildId);

                return (true,
                    $"🌧️ **{item.Name}** kích hoạt! Thời tiết **Mưa** trong **{duration} phút**.\n" +
                    $"✅ Cá cắn câu nhiều hơn **10%** — sản lượng cao hơn\n" +
                    $"✅ Tỉ lệ Hiếm **+15%** | Huyền Thoại **+5%**",
                    item);
            }

            default:
                return (false,
                    $"❌ **{item.Name}** không thể dùng trực tiếp bằng lệnh này.",
                    null);
        }
    }

    // ── /shop repair-rod ─────────────────────────────────────────────────────

    /// <summary>
    /// Sửa chữa cần câu. rodName = null → sửa cần câu hư nhất.
    /// Trả về (success, message, coinsPaid, item, coinsRemaining).
    /// </summary>
    public async Task<(bool success, string message, long coinsPaid, InventoryItem? item, long coinsRemaining)>
        RepairRodAsync(ulong guildId, ulong userId, string? rodName)
    {
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var rods   = await _shopRepo.GetUserRodsAsync(wallet.Id);

        if (rods.Count == 0)
            return (false, "Bạn chưa có cần câu nào.", 0, null, wallet.Coins);

        // Tìm rod cần repair
        UserInventory? target;
        if (rodName is not null)
        {
            target = rods.FirstOrDefault(r =>
                string.Equals(r.Item.Name, rodName, StringComparison.OrdinalIgnoreCase));
            if (target is null)
                return (false, $"Không tìm thấy cần câu **{rodName}** trong túi đồ.", 0, null, wallet.Coins);
        }
        else
        {
            // Ưu tiên rod gãy, sau đó rod hư hao nhất
            target = rods.FirstOrDefault(r => r.RodDurability == 0)
                  ?? rods.Where(r => r.RodDurability.HasValue && r.Item.MaxDurability.HasValue)
                         .OrderBy(r => (double)r.RodDurability!.Value / r.Item.MaxDurability!.Value)
                         .FirstOrDefault();
        }

        if (target is null)
            return (false, "Không tìm thấy cần câu nào cần repair.", 0, null, wallet.Coins);

        if (!target.RodDurability.HasValue || !target.Item.MaxDurability.HasValue)
            return (false, $"**{target.Item.Name}** không hỗ trợ durability tracking (cần câu cũ).", 0, null, wallet.Coins);

        if (target.RodDurability >= target.Item.MaxDurability)
            return (false, $"**{target.Item.Name}** vẫn còn nguyên vẹn (**{target.RodDurability}/{target.Item.MaxDurability}** độ bền).", 0, null, wallet.Coins);

        var cost = GetRepairCost(target.Item.Price, target.Item.MaxDurability.Value, target.RodDurability.Value);
        if (wallet.Coins < cost)
            return (false,
                $"Không đủ xu. Sửa **{target.Item.Name}** ({target.RodDurability}/{target.Item.MaxDurability} → {target.Item.MaxDurability}) cần **{cost:N0}** xu, bạn có **{wallet.Coins:N0}** xu.",
                0, null, wallet.Coins);

        var oldDur = target.RodDurability.Value;
        target.RodDurability = target.Item.MaxDurability.Value;
        wallet.Coins        -= cost;

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId      = guildId,
            FromWalletId = wallet.Id,
            Amount       = cost,
            Type         = TransactionType.Purchase,
            Note         = $"Repair {target.Item.Name} ({oldDur} → {target.Item.MaxDurability})",
            CreatedAt    = DateTime.UtcNow
        });
        await _walletRepo.SaveChangesAsync();
        await _shopRepo.SaveChangesAsync();

        return (true,
            $"🔧 **{target.Item.Emoji} {target.Item.Name}** đã được sửa chữa!\n" +
            $"Độ bền: **{oldDur} → {target.Item.MaxDurability}** ✅\n" +
            $"Đã trả: **{cost:N0}** xu",
            cost, target.Item, wallet.Coins);
    }

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
