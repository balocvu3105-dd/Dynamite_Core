// src/Dynamite.Core/Interfaces/Repositories/IRolePanelRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IRolePanelRepository : IRepository<RolePanel>
{
    Task<IEnumerable<RolePanel>> GetByGuildIdAsync(ulong guildId, CancellationToken ct = default);

    // Lookup khi user click — cần Items để biết roleId
    Task<RolePanel?> GetByMessageIdAsync(ulong guildId, ulong messageId, CancellationToken ct = default);

    // Lookup item cụ thể từ custom_id
    Task<RolePanelItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default);
}