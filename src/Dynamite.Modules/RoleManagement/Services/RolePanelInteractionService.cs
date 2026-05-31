// src/Dynamite.Modules/RoleManagement/Services/RolePanelInteractionService.cs
namespace Dynamite.Modules.RoleManagement.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Dynamite.Modules.RoleManagement.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Không phải InteractionModule — đây là service thuần xử lý raw events
// Registered as Singleton, dùng IServiceScopeFactory để tạo Scoped DbContext per interaction
public class RolePanelInteractionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RolePanelInteractionService> _logger;

    // custom_id prefix constants — tập trung ở đây, không magic string rải rác
    public const string ButtonPrefix = "rolepanel:btn:";
    public const string SelectPrefix = "rolepanel:sel:";

    public RolePanelInteractionService(
        IServiceScopeFactory scopeFactory,
        ILogger<RolePanelInteractionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleButtonAsync(SocketMessageComponent interaction)
    {
        var customId = interaction.Data.CustomId;
        if (!customId.StartsWith(ButtonPrefix)) return;

        var itemIdStr = customId[ButtonPrefix.Length..];
        if (!Guid.TryParse(itemIdStr, out var itemId))
        {
            _logger.LogWarning("Invalid button custom_id: {CustomId}", customId);
            return;
        }

        await ToggleRoleAsync(interaction, itemId);
    }

    public async Task HandleSelectAsync(SocketMessageComponent interaction)
    {
        var customId = interaction.Data.CustomId;
        if (!customId.StartsWith(SelectPrefix)) return;

        // Select menu trả về list các values được chọn
        // Mỗi value = itemId của RolePanelItem
        var selectedIds = interaction.Data.Values
            .Select(v => Guid.TryParse(v, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        // Defer trước — lookup DB có thể mất thời gian
        await interaction.DeferAsync(ephemeral: true);

        using var scope = _scopeFactory.CreateScope();
        var panelService = scope.ServiceProvider.GetRequiredService<IRolePanelService>();

        var guildUser = interaction.User as IGuildUser;
        if (guildUser is null) return;

        var results = new List<string>();

        foreach (var itemId in selectedIds)
        {
            var item = await panelService.GetItemAsync(itemId);
            if (item is null) continue;

            var hasRole = guildUser.RoleIds.Contains(item.RoleId);
            try
            {
                if (hasRole)
                {
                    await guildUser.RemoveRoleAsync(item.RoleId);
                    results.Add($"✖ Removed **{item.Label}**");
                }
                else
                {
                    await guildUser.AddRoleAsync(item.RoleId);
                    results.Add($"✔ Added **{item.Label}**");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle role {RoleId} for user {UserId}",
                    item.RoleId, guildUser.Id);
                results.Add($"✖ Failed to update **{item.Label}**");
            }
        }

        var summary = results.Count > 0
            ? string.Join("\n", results)
            : "No changes made.";

        await interaction.FollowupAsync(
            embed: RoleManagementEmbeds.Info("Roles Updated", summary),
            ephemeral: true);
    }

    private async Task ToggleRoleAsync(SocketMessageComponent interaction, Guid itemId)
    {
        await interaction.DeferAsync(ephemeral: true);

        using var scope = _scopeFactory.CreateScope();
        var panelService = scope.ServiceProvider.GetRequiredService<IRolePanelService>();

        var item = await panelService.GetItemAsync(itemId);
        if (item is null)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Not Found",
                    "This role no longer exists in the panel configuration."),
                ephemeral: true);
            return;
        }

        var guildUser = interaction.User as IGuildUser;
        if (guildUser is null) return;

        var hasRole = guildUser.RoleIds.Contains(item.RoleId);

        try
        {
            if (hasRole)
            {
                await guildUser.RemoveRoleAsync(item.RoleId);
                await interaction.FollowupAsync(
                    embed: RoleManagementEmbeds.Warn("Role Removed", $"**{item.Label}** has been removed."),
                    ephemeral: true);
            }
            else
            {
                await guildUser.AddRoleAsync(item.RoleId);
                await interaction.FollowupAsync(
                    embed: RoleManagementEmbeds.Success("Role Added", $"**{item.Label}** has been assigned."),
                    ephemeral: true);
            }
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == Discord.DiscordErrorCode.UnknownRole)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Role Deleted",
                    $"The role **{item.Label}** no longer exists on this server. Please ask an admin to update the panel."),
                ephemeral: true);
        }
    }
}