// src/Dynamite.Core/Interfaces/Repositories/IAutoRoleRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IAutoRoleRepository : IRepository<AutoRoleConfig>
{
    Task<IEnumerable<AutoRoleConfig>> GetByGuildIdAsync(ulong guildId, CancellationToken ct = default);
    Task<AutoRoleConfig?> GetByGuildAndRoleAsync(ulong guildId, ulong roleId, CancellationToken ct = default);
}