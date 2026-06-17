namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Enums;

public interface IModerationRepository : IRepository<ModerationAction>
{
    /// <summary>Lịch sử hành động bị áp dụng LÊN một user (target).</summary>
    Task<IEnumerable<ModerationAction>> GetUserHistoryAsync(
        ulong guildId, ulong userId,
        int count = 20, CancellationToken ct = default);

    /// <summary>Lịch sử hành động DO một mod thực hiện.</summary>
    Task<IEnumerable<ModerationAction>> GetByModeratorAsync(
        ulong guildId, ulong moderatorId,
        int count = 20, CancellationToken ct = default);

    /// <summary>Các hành động gần nhất trong guild, có thể lọc theo loại.</summary>
    Task<IEnumerable<ModerationAction>> GetRecentActionsAsync(
        ulong guildId, int count = 10,
        ModerationActionType? type = null,
        CancellationToken ct = default);
}
