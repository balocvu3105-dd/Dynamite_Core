// src/Dynamite.Core/Interfaces/Repositories/IFishEncyclopediaRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

/// <summary>
/// Repository cho Fish Encyclopedia.
/// Mỗi entry = 1 loài cá user đã từng câu (per guild).
/// Upsert mỗi lần catch thành công — tăng TimesCaught, cập nhật BestCoins/LastCaughtAt.
/// </summary>
public interface IFishEncyclopediaRepository
{
    /// <summary>
    /// Upsert 1 entry: nếu đã có thì tăng TimesCaught + cập nhật BestCoins/LastCaughtAt,
    /// nếu chưa có thì tạo mới.
    /// </summary>
    Task UpsertAsync(
        ulong guildId, ulong userId,
        string fishName, string emoji, string rarity,
        long coins);

    /// <summary>Lấy toàn bộ encyclopedia của user trong guild, sorted by Rarity order rồi Name.</summary>
    Task<List<FishEncyclopediaEntry>> GetUserDexAsync(ulong guildId, ulong userId);

    /// <summary>Đếm số loài unique user đã câu.</summary>
    Task<int> CountSpeciesAsync(ulong guildId, ulong userId);

    Task SaveChangesAsync();
}
