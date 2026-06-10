// src/Dynamite.Infrastructure/Repositories/WalletRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _db;

    public WalletRepository(AppDbContext db) => _db = db;

    public Task<UserWallet?> GetAsync(ulong guildId, ulong userId)
        => _db.UserWallets
            .Include(w => w.Inventory).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(w => w.GuildId == guildId && w.UserId == userId);

    public async Task<UserWallet> GetOrCreateAsync(ulong guildId, ulong userId)
    {
        var wallet = await GetAsync(guildId, userId);
        if (wallet is not null) return wallet;

        wallet = new UserWallet { GuildId = guildId, UserId = userId };
        await _db.UserWallets.AddAsync(wallet);
        await _db.SaveChangesAsync();
        return wallet;
    }

    public Task<List<UserWallet>> GetLeaderboardAsync(ulong guildId, int top = 10)
        => _db.UserWallets
            .Where(w => w.GuildId == guildId)
            .OrderByDescending(w => w.Coins)
            .Take(top)
            .ToListAsync();

    public async Task AddTransactionAsync(Transaction transaction)
        => await _db.Transactions.AddAsync(transaction);

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}