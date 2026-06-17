// src/Dynamite.Infrastructure/Repositories/FishingLogRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class FishingLogRepository : IFishingLogRepository
{
    private readonly AppDbContext _db;
    public FishingLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(FishingActivityLog log)
        => await _db.FishingActivityLogs.AddAsync(log);

    public Task<List<FishingActivityLog>> GetUserLogsAsync(
        ulong guildId, ulong userId,
        int limit = 20,
        FishingEvent? eventFilter = null)
    {
        var q = _db.FishingActivityLogs
            .Where(l => l.GuildId == guildId && l.UserId == userId);

        if (eventFilter.HasValue)
            q = q.Where(l => l.Event == eventFilter.Value);

        return q.OrderByDescending(l => l.CreatedAt)
                .Take(limit)
                .ToListAsync();
    }

    public Task<List<FishingActivityLog>> GetGuildLogsAsync(
        ulong guildId,
        int limit = 50,
        FishingEvent? eventFilter = null)
    {
        var q = _db.FishingActivityLogs.Where(l => l.GuildId == guildId);

        if (eventFilter.HasValue)
            q = q.Where(l => l.Event == eventFilter.Value);

        return q.OrderByDescending(l => l.CreatedAt)
                .Take(limit)
                .ToListAsync();
    }

    public async Task PruneOldLogsAsync(int olderThanDays = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        await _db.FishingActivityLogs
            .Where(l => l.CreatedAt < cutoff)
            .ExecuteDeleteAsync();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
