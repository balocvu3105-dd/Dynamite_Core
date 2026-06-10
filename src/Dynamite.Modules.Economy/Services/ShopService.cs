// src/Dynamite.Modules.Economy/Services/ShopService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class ShopService
{
    private readonly IWalletRepository _walletRepo;
    private readonly IShopRepository _shopRepo;
    private readonly ILogger<ShopService> _logger;

    public ShopService(
        IWalletRepository walletRepo,
        IShopRepository shopRepo,
        ILogger<ShopService> logger)
    {
        _walletRepo = walletRepo;
        _shopRepo = shopRepo;
        _logger = logger;
    }

    public Task<List<InventoryItem>> GetShopItemsAsync(ulong guildId)
        => _shopRepo.GetAvailableItemsAsync(guildId);

    public async Task<(bool success, string message)> BuyAsync(
        ulong guildId, ulong userId, string itemName)
    {
        var item = await _shopRepo.GetItemByNameAsync(guildId, itemName);
        if (item is null)
            return (false, $"Item **{itemName}** not found in the shop.");

        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);

        if (wallet.Coins < item.Price)
            return (false, $"Insufficient balance. You need **{item.Price:N0}** coins but have **{wallet.Coins:N0}**.");

        // Check nếu đã có item (non-stackable types như FishingRod)
        var existing = await _shopRepo.GetUserItemAsync(wallet.Id, item.Id);
        if (existing is not null && item.Type == ItemType.FishingRod)
            return (false, $"You already own **{item.Name}**.");

        wallet.Coins -= item.Price;

        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            await _shopRepo.AddUserInventoryAsync(new UserInventory
            {
                WalletId = wallet.Id,
                ItemId = item.Id,
                Quantity = 1,
                AcquiredAt = DateTime.UtcNow
            });
        }

        var tx = new Transaction
        {
            GuildId = guildId,
            FromWalletId = wallet.Id,
            Amount = item.Price,
            Type = TransactionType.Purchase,
            Note = $"Purchased {item.Name}",
            CreatedAt = DateTime.UtcNow
        };

        await _walletRepo.AddTransactionAsync(tx);
        await _walletRepo.SaveChangesAsync();

        _logger.LogInformation("User {UserId} bought {Item} for {Price} coins", userId, item.Name, item.Price);
        return (true, $"✅ You bought {item.Emoji} **{item.Name}** for **{item.Price:N0}** coins!");
    }

    public async Task<List<UserInventory>> GetInventoryAsync(ulong guildId, ulong userId)
    {
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        return await _shopRepo.GetUserInventoryAsync(wallet.Id);
    }

    public async Task<(bool success, string message)> AddShopItemAsync(
        ulong guildId,
        string name,
        string emoji,
        long price,
        string? description,
        ItemType type,
        int? cooldownSeconds,
        double? dropMultiplier)
    {
        var existing = await _shopRepo.GetItemByNameAsync(guildId, name);
        if (existing is not null)
            return (false, $"Item **{name}** already exists.");

        var item = new InventoryItem
        {
            GuildId = guildId,
            Name = name,
            Emoji = emoji,
            Price = price,
            Description = description,
            Type = type,
            IsAvailable = true,
            CooldownSeconds = cooldownSeconds,
            DropMultiplier = dropMultiplier
        };

        await _shopRepo.AddItemAsync(item);
        await _shopRepo.SaveChangesAsync();

        return (true, $"✅ Added **{name}** to the shop.");
    }
}