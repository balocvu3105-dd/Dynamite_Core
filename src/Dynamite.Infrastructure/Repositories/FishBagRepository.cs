// src/Dynamite.Infrastructure/Repositories/FishBagRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class FishBagRepository : IFishBagRepository
{
    private readonly AppDbContext _db;
    public FishBagRepository(AppDbContext db) => _db = db;

    public async Task<UserFishBag> GetOrCreateAsync(ulong guildId, ulong userId)
    {
        var bag = await _db.UserFishBags
            .Include(b => b.Fish)
            .FirstOrDefaultAsync(b => b.GuildId == guildId && b.UserId == userId);

        if (bag is not null) return bag;

        bag = new UserFishBag { GuildId = guildId, UserId = userId };
        await _db.UserFishBags.AddAsync(bag);
        await _db.SaveChangesAsync();
        return bag;
    }

    public async Task AddFishAsync(CaughtFish fish)
        => await _db.CaughtFish.AddAsync(fish);

    public Task RemoveFishAsync(IEnumerable<CaughtFish> fish)
    {
        _db.CaughtFish.RemoveRange(fish);
        return Task.CompletedTask;
    }

    public Task<List<CaughtFish>> GetFishByRarityAsync(Guid bagId, string rarity)
        => _db.CaughtFish
            .Where(f => f.BagId == bagId && f.Rarity == rarity)
            .ToListAsync();

    public Task<int> CountPearlsThisWeekAsync(ulong guildId, PearlType type)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        return _db.GuildPearlLogs
            .CountAsync(p => p.GuildId == guildId
                          && p.PearlType == type
                          && p.CreatedAt >= weekAgo);
    }

    public async Task AddPearlLogAsync(GuildPearlLog log)
        => await _db.GuildPearlLogs.AddAsync(log);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
