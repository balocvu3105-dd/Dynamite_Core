// src/Dynamite.Core/Interfaces/Repositories/IAntiSpamRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IAntiSpamRepository : IRepository<AntiSpamConfig>
{
    Task<AntiSpamConfig?> GetByGuildIdAsync(ulong guildId, CancellationToken ct = default);
    Task<AntiSpamConfig> GetOrCreateAsync(ulong guildId, Guid guildConfigId, CancellationToken ct = default);
}