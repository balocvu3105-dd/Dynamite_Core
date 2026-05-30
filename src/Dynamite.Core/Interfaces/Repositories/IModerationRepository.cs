namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IModerationRepository : IRepository<ModerationAction>
{
    Task<IEnumerable<ModerationAction>> GetUserHistoryAsync(ulong guildId, ulong userId, CancellationToken ct = default);
    Task<IEnumerable<ModerationAction>> GetRecentActionsAsync(ulong guildId, int count = 10, CancellationToken ct = default);
}
