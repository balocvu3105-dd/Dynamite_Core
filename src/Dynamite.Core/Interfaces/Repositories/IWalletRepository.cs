// src/Dynamite.Core/Interfaces/Repositories/IWalletRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IWalletRepository
{
    Task<UserWallet?> GetAsync(ulong guildId, ulong userId);
    Task<UserWallet> GetOrCreateAsync(ulong guildId, ulong userId);
    Task<List<UserWallet>> GetLeaderboardAsync(ulong guildId, int top = 10);
    Task AddTransactionAsync(Transaction transaction);
    Task SaveChangesAsync();
}