// src/Dynamite.Core/Interfaces/Repositories/IFishingLogRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IFishingLogRepository
{
    Task AddAsync(FishingActivityLog log);

    Task<List<FishingActivityLog>> GetUserLogsAsync(
        ulong guildId, ulong userId,
        int limit = 20,
        FishingEvent? eventFilter = null);

    Task<List<FishingActivityLog>> GetGuildLogsAsync(
        ulong guildId,
        int limit = 50,
        FishingEvent? eventFilter = null);

    /// <summary>Xóa logs cũ hơn N ngày (cleanup job).</summary>
    Task PruneOldLogsAsync(int olderThanDays = 90);

    Task SaveChangesAsync();
}
