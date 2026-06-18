// src/Dynamite.Application/Interfaces/IRolePanelService.cs
namespace Dynamite.Application.Interfaces;
using Dynamite.Core.Common;
using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
public interface IRolePanelService
{
    Task<RolePanel> CreatePanelAsync(
        ulong guildId, string guildName,
        ulong channelId, ulong messageId,
        string title, string? description,
        RolePanelType panelType,
        IEnumerable<RolePanelItemDto> items,
        int maxRoles = 0,
        CancellationToken ct = default);
    Task UpdateMessageIdAsync(Guid panelId, ulong messageId, CancellationToken ct = default);
    Task DeletePanelAsync(Guid panelId, CancellationToken ct = default);
    Task<IEnumerable<RolePanel>> GetPanelsAsync(ulong guildId, CancellationToken ct = default);
    Task<RolePanelItem?> GetItemAsync(Guid itemId, CancellationToken ct = default);
    Task<RolePanel?> GetPanelByItemAsync(Guid itemId, CancellationToken ct = default);
    Task<ServiceResult<RolePanel>> AddItemAsync(
        Guid panelId, RolePanelItemDto item, CancellationToken ct = default);
}
// DTO để truyền item data từ module xuống service
// Không expose Discord types vào Application layer
public record RolePanelItemDto(
    ulong RoleId,
    string Label,
    string? Emoji,
    string? Description);