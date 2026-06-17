// src/Dynamite.Core/Interfaces/Repositories/IFishingSnapshotRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IFishingSnapshotRepository
{
    Task<List<FishingDataSnapshot>> GetUserSnapshotsAsync(ulong guildId, ulong userId);
    Task<FishingDataSnapshot?> GetByIdAsync(Guid snapshotId);
    Task AddAsync(FishingDataSnapshot snapshot);

    /// <summary>
    /// Xóa snapshots cũ, giữ lại N gần nhất per user.
    /// Gọi sau mỗi lần AddAsync để kiểm soát storage.
    /// </summary>
    Task PruneExcessAsync(ulong guildId, ulong userId, int keepCount = 5);

    Task SaveChangesAsync();
}
