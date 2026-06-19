// src/Dynamite.Modules.Economy/Services/ShopService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Common;
using Dynamite.Core.Common.Results;
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

    private async Task<(bool success, string message)> BuyAsync(
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
            var bagResult  = await _bagService.AddSlotsAsync(guildId, userId, slotsToAdd);
            if (!bagResult) return (false, $"🎒 Túi cá đã đạt tối đa **{MaxBagCap}** slot!");

            var oldCap = bagResult.Value!.OldCapacity;
            var newCap = bagResult.Value.NewCapacity;

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
    /// Mua item và trả về thông tin đầy đủ cho Command và InvoiceService.
    /// Gọi trực tiếp BuyAsync và capture snapshot coins trước/sau để tính coinsPaid.
    /// </summary>
    public async Task<ServiceResult<BuyResult>>
        BuyWithDetailsAsync(ulong guildId, ulong userId, string itemName)
    {
        // Pre-validation (fast-fail trước khi vào BuyAsync để tránh load wallet thêm lần nữa)
        var item = await _shopRepo.GetItemByNameAsync(guildId, itemName);
        if (item is null)
            return ServiceResult<BuyResult>.Fail($"Không tìm thấy **{itemName}** trong cửa hàng.");

        if (item.Price <= 0 && item.Type != ItemType.BagUpgrade)
            return ServiceResult<BuyResult>.Fail("Vật phẩm này không thể mua.");

        // Snapshot coins trước khi mua để tính coinsPaid chính xác
        // (BagUpgrade dùng dynamicPrice nên không thể dùng item.Price)
        var wallet      = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var coinsBefore = wallet.Coins;

        if (item.Type != ItemType.BagUpgrade && wallet.Coins < item.Price)
            return ServiceResult<BuyResult>.Fail(
                $"Không đủ coins. Cần **{item.Price:N0}** nhưng bạn chỉ có **{wallet.Coins:N0}**.");

        // Delegate to private BuyAsync — nó sẽ load lại wallet/item từ EF tracker (cache hit, không tốn thêm roundtrip)
        var (success, message) = await BuyAsync(guildId, userId, itemName);
        if (!success) return ServiceResult<BuyResult>.Fail(message);

        // Sau BuyAsync, wallet entity đã được update in-memory bởi EF tracker
        // → dùng lại wallet.Coins (đã reflect deduction) thay vì GetOrCreateAsync lần 3
        var coinsPaid = coinsBefore - wallet.Coins;
        return ServiceResult<BuyResult>.Ok(
            new BuyResult(item, coinsPaid, wallet.Coins, message));
    }

    public async Task<List<UserInventory>> GetInventoryAsync(ulong guildId, ulong userId)
    {
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        return await _shopRepo.GetUserInventoryAsync(wallet.Id);
    }

    /// <summary>
    /// Trả về danh sách cần câu của user dưới dạng (Name, Label) để dùng trong RodAutocomplete.
    /// Ưu tiên cần gãy → mòn → còn tốt.
    /// Label format: "{emoji} {name} — {dur}/{max}" hoặc "💔 {name} — GÃY" nếu dur == 0.
    /// </summary>
    public async Task<List<(string Name, string Label)>>
        GetUserRodsForAutocompleteAsync(ulong guildId, ulong userId)
    {
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var rods   = await _shopRepo.GetUserRodsAsync(wallet.Id);

        return rods
            // Include broken rods too (durability == 0) — user cần biết để repair
            .OrderBy(r =>
            {
                if (r.RodDurability == 0) return 0;                  // gãy → lên trước
                if (r.RodDurability.HasValue && r.Item.MaxDurability.HasValue
                    && r.RodDurability.Value <= (int)(r.Item.MaxDurability.Value * 0.2))
                    return 1;                                         // mòn nặng ≤ 20%
                return 2;                                             // còn tốt
            })
            .ThenByDescending(r => r.Item.DropMultiplier)
            .Select(r =>
            {
                var dur    = r.RodDurability;
                var max    = r.Item.MaxDurability;
                var label  = dur.HasValue && max.HasValue
                    ? dur.Value == 0
                        ? $"💔 {r.Item.Emoji} {r.Item.Name} — GÃY"
                        : dur.Value <= (int)(max.Value * 0.2)
                            ? $"⚠️ {r.Item.Emoji} {r.Item.Name} — {dur.Value}/{max.Value} (mòn)"
                            : $"{r.Item.Emoji} {r.Item.Name} — {dur.Value}/{max.Value}"
                    : $"{r.Item.Emoji} {r.Item.Name}";
                return (r.Item.Name, label);
            })
            .ToList();
    }

    /// <summary>
    /// Xem trước chi phí sửa mà KHÔNG thực hiện repair. Dùng cho preview mode.
    /// </summary>
    public async Task<ServiceResult<(UserInventory Rod, long Cost, long Coins)>>
        PreviewRepairAsync(ulong guildId, ulong userId, string? rodName)
    {
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var rods   = await _shopRepo.GetUserRodsAsync(wallet.Id);

        if (rods.Count == 0)
            return ServiceResult<(UserInventory Rod, long Cost, long Coins)>.Fail("Bạn chưa có cần câu nào.");

        UserInventory? target = rodName is not null
            ? rods.FirstOrDefault(r => string.Equals(r.Item.Name, rodName, StringComparison.OrdinalIgnoreCase))
            : rods.FirstOrDefault(r => r.RodDurability == 0)
              ?? rods.Where(r => r.RodDurability.HasValue && r.Item.MaxDurability.HasValue)
                     .OrderBy(r => (double)r.RodDurability!.Value / r.Item.MaxDurability!.Value)
                     .FirstOrDefault();

        if (target is null)
            return ServiceResult<(UserInventory Rod, long Cost, long Coins)>.Fail(
                rodName is not null
                    ? $"Không tìm thấy cần câu **{rodName}** trong túi đồ."
                    : "Không tìm thấy cần câu nào cần repair.");

        if (!target.RodDurability.HasValue || !target.Item.MaxDurability.HasValue)
            return ServiceResult<(UserInventory Rod, long Cost, long Coins)>.Fail(
                $"**{target.Item.Name}** không hỗ trợ durability tracking.");

        if (target.RodDurability >= target.Item.MaxDurability)
            return ServiceResult<(UserInventory Rod, long Cost, long Coins)>.Fail(
                $"**{target.Item.Name}** vẫn còn nguyên vẹn ({target.RodDurability}/{target.Item.MaxDurability}).");

        var cost = GetRepairCost(
            target.Item.Price,
            target.Item.MaxDurability.Value,
            target.RodDurability.Value);

        return ServiceResult<(UserInventory Rod, long Cost, long Coins)>.Ok((target, cost, wallet.Coins));
    }

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
                existing.LuckBonus       = item.LuckBonus;
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
            Name            = "Cần Câu Tân Thủ",
            Emoji           = "🎋",
            Price           = 0,
            Description     = "Cần câu miễn phí cho người mới. Stats mặc định — điểm khởi đầu trước khi nâng cấp. Không theo dõi độ bền.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 25,
            DropMultiplier  = 1.0,
            MissRate        = 0.15,
            EscapeRate      = 0.10,
            MaxDurability   = null,  // không gãy — không khuyến khích nâng cấp bằng durability
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Tre",
            Emoji           = "🪁",
            Price           = 3_000,
            Description     = "Cần câu cơ bản. Giảm nhẹ tỉ lệ hụt, tăng nhẹ giá trị cá. Độ bền 200 lần câu.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 22,
            DropMultiplier  = 1.1,
            MissRate        = 0.13,
            EscapeRate      = 0.09,
            MaxDurability   = 200,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Bạc",
            Emoji           = "🎣",
            Price           = 20_000,
            Description     = "Cần câu nâng cấp. Tăng giá trị cá và giảm tỉ lệ hụt đáng kể. Độ bền 300 lần câu.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 19,
            DropMultiplier  = 1.25,
            MissRate        = 0.11,
            EscapeRate      = 0.08,
            MaxDurability   = 300,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Vàng",
            Emoji           = "🏆",
            Price           = 60_000,
            Description     = "Cần câu cao cấp. Hiệu quả rõ rệt — đầu tư cho người chơi nghiêm túc. Độ bền 600 lần câu.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 15,
            DropMultiplier  = 1.55,
            MissRate        = 0.08,
            EscapeRate      = 0.06,
            MaxDurability   = 600,
        },
        new InventoryItem
        {
            GuildId         = guildId,
            Name            = "Cần Câu Kim Cương",
            Emoji           = "💎",
            Price           = 160_000,
            Description     = "Cần câu huyền thoại. End-game — sản lượng và giá trị vượt trội. +1 Điểm May Mắn (Rare+Legendary tăng đáng kể). Độ bền 1000 lần câu.",
            Type            = ItemType.FishingRod,
            IsAvailable     = true,
            CooldownSeconds = 10,
            DropMultiplier  = 2.0,
            MissRate        = 0.05,
            EscapeRate      = 0.04,
            MaxDurability   = 1000,
            LuckBonus       = 1,
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
    /// </summary>
    public async Task<ServiceResult<UseItemResult>> UseItemAsync(
        ulong guildId, ulong userId, string itemName)
    {
        var wallet    = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var inventory = await _shopRepo.GetUserInventoryAsync(wallet.Id);

        var userItem = inventory.FirstOrDefault(i =>
            string.Equals(i.Item.Name, itemName, StringComparison.OrdinalIgnoreCase)
            && i.Quantity > 0);

        if (userItem is null)
            return ServiceResult<UseItemResult>.Fail($"Bạn không có **{itemName}** trong túi đồ.");

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

                var effect =
                    $"🌧️ **{item.Name}** kích hoạt! Thời tiết **Mưa** trong **{duration} phút**.\n" +
                    $"✅ Cá cắn câu nhiều hơn **10%** — sản lượng cao hơn\n" +
                    $"✅ Tỉ lệ Hiếm **+15%** | Huyền Thoại **+5%**";
                return ServiceResult<UseItemResult>.Ok(new UseItemResult(item, effect));
            }

            default:
                return ServiceResult<UseItemResult>.Fail(
                    $"**{item.Name}** không thể dùng trực tiếp bằng lệnh này.");
        }
    }


    // ── Rod Repair ────────────────────────────────────────────────────────────

    /// <summary>
    /// Sửa cần câu: phục hồi durability về MaxDurability, trừ xu tỉ lệ hư hỏng.
    /// Nếu <paramref name="rodName"/> null → chọn cần hư nhất (priority: gãy → durability thấp nhất).
    /// </summary>
    public async Task<ServiceResult<RepairRodResult>> RepairRodAsync(
        ulong guildId, ulong userId, string? rodName)
    {
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var rods   = await _shopRepo.GetUserRodsAsync(wallet.Id);

        if (rods.Count == 0)
            return ServiceResult<RepairRodResult>.Fail("Bạn chưa có cần câu nào.");

        UserInventory? target = rodName is not null
            ? rods.FirstOrDefault(r => string.Equals(r.Item.Name, rodName, StringComparison.OrdinalIgnoreCase))
            : rods.FirstOrDefault(r => r.RodDurability == 0)
              ?? rods.Where(r => r.RodDurability.HasValue && r.Item.MaxDurability.HasValue)
                     .OrderBy(r => (double)r.RodDurability!.Value / r.Item.MaxDurability!.Value)
                     .FirstOrDefault();

        if (target is null)
            return ServiceResult<RepairRodResult>.Fail(
                rodName is not null
                    ? $"Không tìm thấy cần câu **{rodName}** trong túi đồ."
                    : "Không tìm thấy cần câu nào cần repair.");

        if (!target.RodDurability.HasValue || !target.Item.MaxDurability.HasValue)
            return ServiceResult<RepairRodResult>.Fail(
                $"**{target.Item.Name}** không hỗ trợ durability tracking.");

        if (target.RodDurability >= target.Item.MaxDurability)
            return ServiceResult<RepairRodResult>.Fail(
                $"**{target.Item.Name}** vẫn còn nguyên vẹn ({target.RodDurability}/{target.Item.MaxDurability}).");

        var cost = GetRepairCost(
            target.Item.Price,
            target.Item.MaxDurability.Value,
            target.RodDurability.Value);

        if (wallet.Coins < cost)
            return ServiceResult<RepairRodResult>.Fail(
                $"Không đủ xu. Cần **{cost:N0}** để sửa nhưng bạn chỉ có **{wallet.Coins:N0}** xu.");

        var oldDurability = target.RodDurability.Value;
        var maxDurability = target.Item.MaxDurability.Value;

        target.RodDurability = maxDurability;
        wallet.Coins        -= cost;

        await _walletRepo.SaveChangesAsync();
        await _shopRepo.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} repaired {Rod} ({Old}->{Max}) for {Cost} coins in guild {GuildId}",
            userId, target.Item.Name, oldDurability, maxDurability, cost, guildId);

        return ServiceResult<RepairRodResult>.Ok(new RepairRodResult(
            Item:           target.Item,
            OldDurability:  oldDurability,
            NewDurability:  maxDurability,
            CoinsPaid:      cost,
            CoinsRemaining: wallet.Coins));
    }

    // ── Add Shop Item (Admin) ─────────────────────────────────────────────────

    /// <summary>
    /// Thêm item mới vào shop thủ công (Admin command /shop additem).
    /// Không cho phép trùng tên trong cùng guild.
    /// </summary>
    public async Task<ServiceResult<bool>> AddShopItemAsync(
        ulong guildId, string name, string emoji, long price,
        string? description, ItemType type,
        int? cooldown, double? multiplier,
        int? usageCount, int? durationMinutes)
    {
        var existing = await _shopRepo.GetItemByNameAsync(guildId, name);
        if (existing is not null)
            return ServiceResult<bool>.Fail($"**{name}** đã tồn tại trong cửa hàng. Dùng /shop seed để sync hoặc xóa thủ công.");

        var item = new InventoryItem
        {
            GuildId         = guildId,
            Name            = name,
            Emoji           = emoji,
            Price           = price,
            Description     = description,
            Type            = type,
            IsAvailable     = true,
            CooldownSeconds = cooldown,
            DropMultiplier  = multiplier,
            UsageCount      = usageCount,
            DurationMinutes = durationMinutes,
        };

        await _shopRepo.AddItemAsync(item);
        await _shopRepo.SaveChangesAsync();

        _logger.LogInformation(
            "Admin added shop item [{Name}] price={Price} type={Type} in guild {GuildId}",
            name, price, type, guildId);

        return ServiceResult<bool>.Ok(true);
    }

    // ── Rod Upgrade ───────────────────────────────────────────────────────────

    /// <summary>
    /// Nâng cấp cần câu theo path:
    ///   Bạc → Vàng    : LegendaryCaught ≥ 10, phí 25,000 xu  (tiết kiệm 35k vs mua thẳng)
    ///   Vàng → Kim Cương : LegendaryCaught ≥ 30, phí 80,000 xu  (tiết kiệm 80k vs mua thẳng)
    ///
    /// Caller truyền <paramref name="legendaryCaught"/> từ UserFishingProfile (tránh inject thêm repo).
    /// </summary>
    public async Task<ServiceResult<RodUpgradeResult>> UpgradeRodAsync(
        ulong guildId, ulong userId, string fromRodName, int legendaryCaught)
    {
        // ── Định nghĩa upgrade paths ──────────────────────────────────────────
        var paths = new Dictionary<string, (string ToRod, long Cost, int MinLegendary)>
        {
            ["Cần Câu Bạc"]  = ("Cần Câu Vàng",      25_000, 10),
            ["Cần Câu Vàng"] = ("Cần Câu Kim Cương",  80_000, 30),
        };

        if (!paths.TryGetValue(fromRodName, out var path))
            return ServiceResult<RodUpgradeResult>.Fail(
                $"**{fromRodName}** không có con đường nâng cấp. (Bạc -> Vàng, Vàng -> Kim Cương)");

        var (toRodName, upgradeCost, minLegendary) = path;

        // ── Check LegendaryCaught ─────────────────────────────────────────────
        if (legendaryCaught < minLegendary)
            return ServiceResult<RodUpgradeResult>.Fail(
                $"Cần câu được **{minLegendary} Cá Huyền Thoại** để nâng cấp lên {toRodName}. Bạn hiện có: **{legendaryCaught}/{minLegendary}** con. Cố lên!");

        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);

        // ── Check wallet ──────────────────────────────────────────────────────
        if (wallet.Coins < upgradeCost)
            return ServiceResult<RodUpgradeResult>.Fail(
                $"Không đủ xu. Cần **{upgradeCost:N0}** để nâng cấp nhưng bạn chỉ có **{wallet.Coins:N0}** xu.");

        // ── Check sở hữu cần cũ ───────────────────────────────────────────────
        var fromItem = await _shopRepo.GetItemByNameAsync(guildId, fromRodName);
        if (fromItem is null)
            return ServiceResult<RodUpgradeResult>.Fail($"Không tìm thấy **{fromRodName}** trong cửa hàng.");

        var fromInv = await _shopRepo.GetUserItemAsync(wallet.Id, fromItem.Id);
        if (fromInv is null)
            return ServiceResult<RodUpgradeResult>.Fail(
                $"Bạn không có **{fromRodName}** trong túi đồ. Cần câu hiện tại phải là cần muốn nâng cấp.");

        // ── Load cần mới ──────────────────────────────────────────────────────
        var toItem = await _shopRepo.GetItemByNameAsync(guildId, toRodName);
        if (toItem is null)
            return ServiceResult<RodUpgradeResult>.Fail(
                $"Lỗi cấu hình: không tìm thấy **{toRodName}** trong shop. Liên hệ admin.");

        // ── Check chưa sở hữu cần mới (tránh dupe) ───────────────────────────
        var existingNew = await _shopRepo.GetUserItemAsync(wallet.Id, toItem.Id);
        if (existingNew is not null)
            return ServiceResult<RodUpgradeResult>.Fail(
                $"Bạn đã có **{toRodName}** rồi! Không cần nâng cấp.");

        // ── Thực hiện upgrade ─────────────────────────────────────────────────
        wallet.Coins -= upgradeCost;

        // Xóa cần cũ
        await _shopRepo.RemoveUserInventoryAsync(fromInv);

        // Thêm cần mới với durability đầy
        await _shopRepo.AddUserInventoryAsync(new UserInventory
        {
            WalletId      = wallet.Id,
            ItemId        = toItem.Id,
            Quantity      = 1,
            RodDurability = toItem.MaxDurability,
            AcquiredAt    = DateTime.UtcNow,
        });

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId      = guildId,
            FromWalletId = wallet.Id,
            Amount       = upgradeCost,
            Type         = TransactionType.Purchase,
            Note         = $"Nâng cấp {fromRodName} → {toRodName}",
            CreatedAt    = DateTime.UtcNow,
        });

        await _walletRepo.SaveChangesAsync();
        await _shopRepo.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} upgraded rod {From} → {To} for {Cost} coins in guild {GuildId}",
            userId, fromRodName, toRodName, upgradeCost, guildId);

        return ServiceResult<RodUpgradeResult>.Ok(new RodUpgradeResult(
            FromRodName:     fromRodName,
            ToRodName:       toRodName,
            ToRodEmoji:      toItem.Emoji,
            UpgradeCost:     upgradeCost,
            CoinsRemaining:  wallet.Coins,
            NewDurability:   toItem.MaxDurability ?? 0));
    }
}
