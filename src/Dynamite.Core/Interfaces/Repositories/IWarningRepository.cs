// src/Dynamite.Core/Interfaces/Repositories/IWarningRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IWarningRepository : IRepository<Warning>
{
    Task<IEnumerable<Warning>> GetActiveWarningsAsync(ulong guildId, ulong userId, CancellationToken ct = default);
    Task<int> GetWarningCountAsync(ulong guildId, ulong userId, CancellationToken ct = default);

    /// <summary>
    /// Fetch a single warning scoped to a guild — prevents cross-guild access.
    /// </summary>
    Task<Warning?> GetByGuildAndIdAsync(ulong guildId, Guid warningId, CancellationToken ct = default);
}