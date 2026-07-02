// src/Dynamite.Core/Interfaces/Repositories/IShopRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IShopRepository
{
    Task<List<InventoryItem>> GetAvailableItemsAsync(ulong guildId);
    Task<InventoryItem?> GetItemByNameAsync(ulong guildId, string name);
    Task<InventoryItem?> GetItemByIdAsync(Guid id);
    Task<InventoryItem?> GetItemByTypeAsync(ulong guildId, ItemType type);
    Task<UserInventory?> GetUserItemAsync(Guid walletId, Guid itemId);
    Task AddItemAsync(InventoryItem item);
    Task AddUserInventoryAsync(UserInventory inventory);
    Task<List<UserInventory>> GetUserInventoryAsync(Guid walletId);
    Task<UserInventory?> GetBestRodAsync(Guid walletId);
    Task<UserInventory?> GetActiveBaitAsync(Guid walletId);
    /// <summary>Lấy tất cả cần câu của user (kể cả gãy).</summary>
    Task<List<UserInventory>> GetUserRodsAsync(Guid walletId);
    Task RemoveUserInventoryAsync(UserInventory inventory);
    Task SaveChangesAsync();
}