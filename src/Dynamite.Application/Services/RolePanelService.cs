// src/Dynamite.Application/Services/RolePanelService.cs
namespace Dynamite.Application.Services;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
public class RolePanelService : IRolePanelService
{
    private readonly IRolePanelRepository _panelRepo;
    private readonly IGuildConfigRepository _guildConfigRepo;
    private readonly ILogger<RolePanelService> _logger;
    public RolePanelService(
        IRolePanelRepository panelRepo,
        IGuildConfigRepository guildConfigRepo,
        ILogger<RolePanelService> logger)
    {
        _panelRepo = panelRepo;
        _guildConfigRepo = guildConfigRepo;
        _logger = logger;
    }
    public async Task<RolePanel> CreatePanelAsync(
        ulong guildId, string guildName,
        ulong channelId, ulong messageId,
        string title, string? description,
        RolePanelType panelType,
        IEnumerable<RolePanelItemDto> items,
        int maxRoles = 0,
        CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        var panel = new RolePanel
        {
            GuildId = guildId,
            ChannelId = channelId,
            MessageId = messageId,
            Title = title,
            Description = description,
            PanelType = panelType,
            MaxRoles = maxRoles < 0 ? 0 : maxRoles,
            GuildConfigId = config.Id,
            Items = items.Select(dto => new RolePanelItem
            {
                RoleId = dto.RoleId,
                Label = dto.Label,
                Emoji = dto.Emoji,
                Description = dto.Description
            }).ToList()
        };
        await _panelRepo.AddAsync(panel, ct);
        await _panelRepo.SaveChangesAsync(ct);
        _logger.LogInformation("RolePanel '{Title}' created in guild {GuildId}, message {MessageId}",
            title, guildId, messageId);
        return panel;
    }
    public async Task UpdateMessageIdAsync(Guid panelId, ulong messageId, CancellationToken ct = default)
    {
        var panel = await _panelRepo.GetByIdAsync(panelId, ct)
            ?? throw new InvalidOperationException($"Panel {panelId} not found.");
        panel.MessageId = messageId;
        panel.UpdatedAt = DateTime.UtcNow;
        await _panelRepo.SaveChangesAsync(ct);
        _logger.LogInformation("RolePanel {PanelId} messageId updated to {MessageId}", panelId, messageId);
    }
    public async Task DeletePanelAsync(Guid panelId, CancellationToken ct = default)
    {
        var panel = await _panelRepo.GetByIdAsync(panelId, ct)
            ?? throw new InvalidOperationException($"Panel {panelId} not found.");
        await _panelRepo.DeleteAsync(panel, ct);
        await _panelRepo.SaveChangesAsync(ct);
        _logger.LogInformation("RolePanel {PanelId} deleted", panelId);
    }
    public async Task<IEnumerable<RolePanel>> GetPanelsAsync(
        ulong guildId, CancellationToken ct = default)
        => await _panelRepo.GetByGuildIdAsync(guildId, ct);
    public async Task<RolePanelItem?> GetItemAsync(
        Guid itemId, CancellationToken ct = default)
        => await _panelRepo.GetItemByIdAsync(itemId, ct);

    public async Task<RolePanel?> GetPanelByItemAsync(
        Guid itemId, CancellationToken ct = default)
        => await _panelRepo.GetPanelByItemIdAsync(itemId, ct);
}