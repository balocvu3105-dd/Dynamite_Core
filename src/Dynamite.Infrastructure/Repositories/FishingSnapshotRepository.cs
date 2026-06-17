// src/Dynamite.Infrastructure/Repositories/FishingSnapshotRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class FishingSnapshotRepository : IFishingSnapshotRepository
{
    private readonly AppDbContext _db;
    public FishingSnapshotRepository(AppDbContext db) => _db = db;

    public Task<List<FishingDataSnapshot>> GetUserSnapshotsAsync(ulong guildId, ulong userId)
        => _db.FishingDataSnapshots
            .Where(s => s.GuildId == guildId && s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public Task<FishingDataSnapshot?> GetByIdAsync(Guid snapshotId)
        => _db.FishingDataSnapshots.FindAsync(snapshotId).AsTask();

    public async Task AddAsync(FishingDataSnapshot snapshot)
        => await _db.FishingDataSnapshots.AddAsync(snapshot);

    public async Task PruneExcessAsync(ulong guildId, ulong userId, int keepCount = 5)
    {
        var all = await _db.FishingDataSnapshots
            .Where(s => s.GuildId == guildId && s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        if (all.Count <= keepCount) return;

        var toDelete = all.Skip(keepCount).ToList();
        _db.FishingDataSnapshots.RemoveRange(toDelete);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
