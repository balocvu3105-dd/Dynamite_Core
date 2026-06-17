// src/Dynamite.Infrastructure/Repositories/LeaderboardRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly AppDbContext _db;
    public LeaderboardRepository(AppDbContext db) => _db = db;

    // ── Snapshots ────────────────────────────────────────────────────────────

    public Task<LeaderboardSnapshot?> GetLatestSnapshotAsync(ulong guildId, LeaderboardType type)
        => _db.LeaderboardSnapshots
            .AsNoTracking()
            .Include(s => s.Entries)
            .Where(s => s.GuildId == guildId && s.Type == type)
            .OrderByDescending(s => s.WeekStartDate)
            .FirstOrDefaultAsync();

    public Task<LeaderboardSnapshot?> GetPreviousSnapshotAsync(ulong guildId, LeaderboardType type)
        => _db.LeaderboardSnapshots
            .AsNoTracking()
            .Include(s => s.Entries)
            .Where(s => s.GuildId == guildId && s.Type == type)
            .OrderByDescending(s => s.WeekStartDate)
            .Skip(1)
            .FirstOrDefaultAsync();

    public async Task AddSnapshotAsync(LeaderboardSnapshot snapshot)
        => await _db.LeaderboardSnapshots.AddAsync(snapshot);

    // ── Weekly activity ───────────────────────────────────────────────────────

    public async Task<WeeklyActivity> GetOrCreateWeeklyActivityAsync(ulong guildId, ulong userId)
    {
        var activity = await _db.WeeklyActivities
            .FirstOrDefaultAsync(a => a.GuildId == guildId && a.UserId == userId);

        if (activity is not null) return activity;

        activity = new WeeklyActivity
        {
            GuildId = guildId,
            UserId = userId,
            WeekResetAt = NextSundayNoon()
        };
        await _db.WeeklyActivities.AddAsync(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public Task<List<WeeklyActivity>> GetTopWeeklyAsync(
        ulong guildId, LeaderboardType type, int top = 10)
    {
        var query = _db.WeeklyActivities.Where(a => a.GuildId == guildId);

        query = type switch
        {
            LeaderboardType.Fishing => query.OrderByDescending(a => a.WeeklyFishCaught),
            LeaderboardType.Chat    => query.OrderByDescending(a => a.WeeklyMessages),
            LeaderboardType.Voice   => query.OrderByDescending(a => a.WeeklyVoiceMinutes),
            _ => query
        };

        return query.AsNoTracking().Take(top).ToListAsync();
    }

    public async Task ResetWeeklyActivitiesAsync(ulong guildId)
    {
        var activities = await _db.WeeklyActivities
            .Where(a => a.GuildId == guildId)
            .ToListAsync();

        foreach (var a in activities)
        {
            a.WeeklyFishCaught   = 0;
            a.WeeklyMessages     = 0;
            a.WeeklyVoiceMinutes = 0;
            a.WeekResetAt        = NextSundayNoon();
        }
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    private static DateTime NextSundayNoon()
    {
        var now = DateTime.UtcNow;
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0 && now.Hour >= 12) daysUntilSunday = 7;
        return now.Date.AddDays(daysUntilSunday).AddHours(12);
    }
}
