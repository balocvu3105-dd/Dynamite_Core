// src/Dynamite.Infrastructure/Repositories/SpecialPoolRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class SpecialPoolRepository : ISpecialPoolRepository
{
    private readonly AppDbContext _db;
    public SpecialPoolRepository(AppDbContext db) => _db = db;

    public Task<List<SpecialPool>> GetActivePoolsAsync(ulong guildId)
        => _db.SpecialPools
            .Where(p => p.GuildId == guildId
                     && p.StartsAt <= DateTime.UtcNow
                     && p.ExpiresAt > DateTime.UtcNow
                     && p.RemainingFish > 0)
            .ToListAsync();

    public Task<SpecialPool?> GetByIdAsync(Guid poolId)
        => _db.SpecialPools.FindAsync(poolId).AsTask();

    public async Task AddPoolAsync(SpecialPool pool)
        => await _db.SpecialPools.AddAsync(pool);

    public Task<int> GetTodayPoolCountAsync(ulong guildId, DateTime utcDate)
    {
        var dayStart = utcDate.Date;
        var dayEnd   = dayStart.AddDays(1);
        return _db.SpecialPools
            .CountAsync(p => p.GuildId == guildId
                          && p.StartsAt >= dayStart
                          && p.StartsAt < dayEnd);
    }

    public Task<int> GetGuildPearlCountAsync(ulong guildId, DateTime since)
        => _db.GuildPearlLogs
            .CountAsync(l => l.GuildId == guildId && l.CreatedAt >= since);

    public async Task AddPearlLogAsync(GuildPearlLog log)
        => await _db.GuildPearlLogs.AddAsync(log);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
