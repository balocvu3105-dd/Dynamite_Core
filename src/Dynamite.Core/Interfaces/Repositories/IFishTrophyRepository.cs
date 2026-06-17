// src/Dynamite.Core/Interfaces/Repositories/IFishTrophyRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IFishTrophyRepository
{
    /// <summary>Thêm trophy nếu chưa có (idempotent).</summary>
    Task TryAddAsync(ulong guildId, ulong userId, string fishName, string rarity,
        bool isPearl = false, bool isSpecial = false);

    /// <summary>Đếm số loài unique Rare+ của user.</summary>
    Task<int> GetCountAsync(ulong guildId, ulong userId);

    /// <summary>Top N user theo số loài unique (Collector leaderboard).</summary>
    Task<List<(ulong UserId, int UniqueCount)>> GetTopCollectorsAsync(ulong guildId, int top = 10);

    /// <summary>Danh sách trophy của user (dùng cho /gallery hoặc /profile sau).</summary>
    Task<List<UserFishTrophy>> GetUserTrophiesAsync(ulong guildId, ulong userId);

    Task SaveChangesAsync();
}
