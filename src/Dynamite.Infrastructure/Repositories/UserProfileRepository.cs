// src/Dynamite.Infrastructure/Repositories/UserProfileRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _db;
    public UserProfileRepository(AppDbContext db) => _db = db;

    // ── Fishing Profile ──────────────────────────────────────────────────────

    public async Task<UserFishingProfile> GetOrCreateFishingAsync(ulong guildId, ulong userId)
    {
        var profile = await _db.UserFishingProfiles
            .Include(p => p.Achievements)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.UserId == userId);

        if (profile is not null) return profile;

        profile = new UserFishingProfile { GuildId = guildId, UserId = userId };
        await _db.UserFishingProfiles.AddAsync(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public Task<List<UserFishingProfile>> GetTopFishersAsync(ulong guildId, int top = 10)
        => _db.UserFishingProfiles
            .Where(p => p.GuildId == guildId)
            .OrderByDescending(p => p.TotalCaught)
            .Take(top)
            .ToListAsync();

    public Task<List<ulong>> GetActiveUserIdsAsync(ulong guildId)
        => _db.UserFishingProfiles
            .Where(p => p.GuildId == guildId && p.TotalCaught > 0)
            .Select(p => p.UserId)
            .ToListAsync();

    public Task<List<UserFishingProfile>> GetAllActiveAutoFishProfilesAsync()
    {
        var now = DateTime.UtcNow;
        return _db.UserFishingProfiles
            .AsNoTracking()
            .Where(p => p.AutoFishExpiresAt != null && p.AutoFishExpiresAt > now)
            .ToListAsync();
    }

    // ── Server Profile ───────────────────────────────────────────────────────

    public async Task<UserServerProfile> GetOrCreateServerAsync(ulong guildId, ulong userId)
    {
        var profile = await _db.UserServerProfiles
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.UserId == userId);

        if (profile is not null) return profile;

        profile = new UserServerProfile { GuildId = guildId, UserId = userId };
        await _db.UserServerProfiles.AddAsync(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    // ── Achievements ─────────────────────────────────────────────────────────

    public Task<bool> HasAchievementAsync(ulong guildId, ulong userId, string achievementId)
        => _db.UserFishingAchievements
            .AnyAsync(a => a.GuildId == guildId && a.UserId == userId && a.AchievementId == achievementId);

    public async Task AddAchievementAsync(UserFishingAchievement achievement)
        => await _db.UserFishingAchievements.AddAsync(achievement);

    // ── Level Roles ──────────────────────────────────────────────────────────

    public Task<List<GuildLevelRole>> GetLevelRolesAsync(ulong guildId, LevelType type)
        => _db.GuildLevelRoles
            .Where(r => r.GuildId == guildId && r.LevelType == type)
            .OrderBy(r => r.RequiredLevel)
            .ToListAsync();

    public async Task AddLevelRoleAsync(GuildLevelRole role)
        => await _db.GuildLevelRoles.AddAsync(role);

    public async Task RemoveLevelRoleAsync(ulong guildId, LevelType type, int level)
    {
        var role = await _db.GuildLevelRoles
            .FirstOrDefaultAsync(r => r.GuildId == guildId
                                   && r.LevelType == type
                                   && r.RequiredLevel == level);
        if (role is not null) _db.GuildLevelRoles.Remove(role);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
