// src/Dynamite.Core/Interfaces/Repositories/ILeaderboardRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface ILeaderboardRepository
{
    // ── Snapshots ────────────────────────────────────────────────────────────
    Task<LeaderboardSnapshot?> GetLatestSnapshotAsync(ulong guildId, LeaderboardType type);
    Task<LeaderboardSnapshot?> GetPreviousSnapshotAsync(ulong guildId, LeaderboardType type);
    Task AddSnapshotAsync(LeaderboardSnapshot snapshot);

    // ── Weekly activity (raw counters, reset sau snapshot) ───────────────────
    Task<WeeklyActivity> GetOrCreateWeeklyActivityAsync(ulong guildId, ulong userId);
    Task<List<WeeklyActivity>> GetTopWeeklyAsync(ulong guildId, LeaderboardType type, int top = 10);
    Task ResetWeeklyActivitiesAsync(ulong guildId);

    Task SaveChangesAsync();
}
