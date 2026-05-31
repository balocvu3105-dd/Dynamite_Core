// src/Dynamite.Application/Interfaces/IAutoRoleService.cs
namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;

public interface IAutoRoleService
{
    Task<AutoRoleConfig> AddAutoRoleAsync(ulong guildId, string guildName, ulong roleId, CancellationToken ct = default);
    Task RemoveAutoRoleAsync(ulong guildId, ulong roleId, CancellationToken ct = default);
    Task<IEnumerable<AutoRoleConfig>> GetAutoRolesAsync(ulong guildId, CancellationToken ct = default);

    // Trả về list roleIds cần assign — Bot layer tự gọi Discord API
    Task<IEnumerable<ulong>> GetRoleIdsToApplyAsync(ulong guildId, CancellationToken ct = default);
}