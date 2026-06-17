// src/Dynamite.Infrastructure/Repositories/ShopRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class ShopRepository : IShopRepository
{
    private readonly AppDbContext _db;

    public ShopRepository(AppDbContext db) => _db = db;

    public Task<List<InventoryItem>> GetAvailableItemsAsync(ulong guildId)
        => _db.InventoryItems
            .Where(i => i.GuildId == guildId && i.IsAvailable)
            .OrderBy(i => i.Price)
            .ToListAsync();

    public Task<InventoryItem?> GetItemByNameAsync(ulong guildId, string name)
        => _db.InventoryItems
            .FirstOrDefaultAsync(i => i.GuildId == guildId &&
                                      i.Name.ToLower() == name.ToLower() &&
                                      i.IsAvailable);

    public Task<InventoryItem?> GetItemByIdAsync(Guid id)
        => _db.InventoryItems.FindAsync(id).AsTask();

    public Task<UserInventory?> GetUserItemAsync(Guid walletId, Guid itemId)
        => _db.UserInventories
            .Include(u => u.Item)
            .FirstOrDefaultAsync(u => u.WalletId == walletId && u.ItemId == itemId);

    public async Task AddItemAsync(InventoryItem item)
        => await _db.InventoryItems.AddAsync(item);

    public async Task AddUserInventoryAsync(UserInventory inventory)
        => await _db.UserInventories.AddAsync(inventory);

    public Task<List<UserInventory>> GetUserInventoryAsync(Guid walletId)
        => _db.UserInventories
            .Include(u => u.Item)
            .Where(u => u.WalletId == walletId)
            .ToListAsync();

    public Task<UserInventory?> GetBestRodAsync(Guid walletId)
        => _db.UserInventories
            .Include(u => u.Item)
            .Where(u => u.WalletId == walletId && u.Item.Type == ItemType.FishingRod)
            .OrderByDescending(u => u.Item.DropMultiplier)
            .FirstOrDefaultAsync();

    public Task RemoveUserInventoryAsync(UserInventory inventory)
    {
        _db.UserInventories.Remove(inventory);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}