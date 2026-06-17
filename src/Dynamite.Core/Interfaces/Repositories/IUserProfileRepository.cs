// src/Dynamite.Core/Interfaces/Repositories/IUserProfileRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IUserProfileRepository
{
    // ── Fishing Profile ──────────────────────────────────────────────────────
    Task<UserFishingProfile> GetOrCreateFishingAsync(ulong guildId, ulong userId);
    Task<List<UserFishingProfile>> GetTopFishersAsync(ulong guildId, int top = 10);
    /// <summary>Trả về userId của tất cả user có TotalCaught > 0 trong guild (dùng cho weekly backup).</summary>
    Task<List<ulong>> GetActiveUserIdsAsync(ulong guildId);

    /// <summary>Trả về tất cả profile đang có auto-fish session chưa hết hạn (cross-guild).</summary>
    Task<List<UserFishingProfile>> GetAllActiveAutoFishProfilesAsync();

    // ── Server Profile ───────────────────────────────────────────────────────
    Task<UserServerProfile> GetOrCreateServerAsync(ulong guildId, ulong userId);

    // ── Achievements ─────────────────────────────────────────────────────────
    Task<bool> HasAchievementAsync(ulong guildId, ulong userId, string achievementId);
    Task AddAchievementAsync(UserFishingAchievement achievement);

    // ── Level Roles ──────────────────────────────────────────────────────────
    Task<List<GuildLevelRole>> GetLevelRolesAsync(ulong guildId, LevelType type);
    Task AddLevelRoleAsync(GuildLevelRole role);
    Task RemoveLevelRoleAsync(ulong guildId, LevelType type, int level);

    Task SaveChangesAsync();
}
