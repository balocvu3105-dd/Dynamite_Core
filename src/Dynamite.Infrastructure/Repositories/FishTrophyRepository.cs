// src/Dynamite.Infrastructure/Repositories/FishTrophyRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class FishTrophyRepository : IFishTrophyRepository
{
    private readonly AppDbContext _db;
    public FishTrophyRepository(AppDbContext db) => _db = db;

    public async Task TryAddAsync(
        ulong guildId, ulong userId, string fishName, string rarity,
        bool isPearl = false, bool isSpecial = false)
    {
        // Unique check — không insert nếu đã có
        var exists = await _db.UserFishTrophies.AnyAsync(t =>
            t.GuildId == guildId && t.UserId == userId && t.FishName == fishName);

        if (exists) return;

        await _db.UserFishTrophies.AddAsync(new UserFishTrophy
        {
            GuildId   = guildId,
            UserId    = userId,
            FishName  = fishName,
            Rarity    = rarity,
            IsPearl   = isPearl,
            IsSpecial = isSpecial,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task<int> GetCountAsync(ulong guildId, ulong userId)
        => _db.UserFishTrophies
            .CountAsync(t => t.GuildId == guildId && t.UserId == userId);

    public async Task<List<(ulong UserId, int UniqueCount)>> GetTopCollectorsAsync(
        ulong guildId, int top = 10)
    {
        var rows = await _db.UserFishTrophies
            .AsNoTracking()
            .Where(t => t.GuildId == guildId)
            .GroupBy(t => t.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync();

        return rows.Select(r => (r.UserId, r.Count)).ToList();
    }

    public Task<List<UserFishTrophy>> GetUserTrophiesAsync(ulong guildId, ulong userId)
        => _db.UserFishTrophies
            .AsNoTracking()
            .Where(t => t.GuildId == guildId && t.UserId == userId)
            .OrderBy(t => t.Rarity)
            .ThenBy(t => t.FishName)
            .ToListAsync();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
