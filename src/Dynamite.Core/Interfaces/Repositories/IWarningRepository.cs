namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IWarningRepository : IRepository<Warning>
{
    Task<IEnumerable<Warning>> GetActiveWarningsAsync(ulong guildId, ulong userId, CancellationToken ct = default);
    Task<int> GetWarningCountAsync(ulong guildId, ulong userId, CancellationToken ct = default);
}