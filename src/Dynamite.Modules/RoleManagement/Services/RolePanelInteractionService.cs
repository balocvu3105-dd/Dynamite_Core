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

        var guildUser = interaction.User as IGuildUser;
        if (guildUser is null && interaction.Channel is IGuildChannel guildChannel)
        {
            guildUser = await guildChannel.Guild.GetUserAsync(interaction.User.Id);
        }

        if (guildUser is null)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Error",
                    "Could not resolve your member profile on this server. Please try again or contact an admin."),
                ephemeral: true);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var panelService = scope.ServiceProvider.GetRequiredService<IRolePanelService>();

        // Load panel MỘT lần (kèm toàn bộ items) — vừa đỡ query lặp,
        // vừa cần để enforce MaxRoles trên tổng role của panel
        var panel = selectedIds.Count > 0
            ? await panelService.GetPanelByItemAsync(selectedIds[0])
            : null;
        if (panel is null)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Not Found",
                    "This panel no longer exists in the bot database (likely due to a database migration/reset or deletion). Please ask an admin to recreate this panel."),
                ephemeral: true);
            return;
        }

        // Đếm số role thuộc panel này mà user ĐANG giữ — cập nhật dần trong vòng lặp
        var heldCount = panel.Items.Count(i => guildUser.RoleIds.Contains(i.RoleId));

        var results = new List<string>();

        foreach (var itemId in selectedIds)
        {
            var item = panel.Items.FirstOrDefault(i => i.Id == itemId);
            if (item is null) continue;

            var role = guildUser.Guild.GetRole(item.RoleId);
            if (role is null)
            {
                results.Add($"✖ **{item.Label}** failed — role deleted on server.");
                continue;
            }

            var socketGuild = ((SocketGuildChannel)interaction.Channel).Guild;
            var botUser = socketGuild.CurrentUser;
            if (botUser is not null && botUser.Hierarchy <= role.Position)
            {
                results.Add($"✖ **{item.Label}** failed — bot role below target role.");
                continue;
            }

            var hasRole = guildUser.RoleIds.Contains(item.RoleId);
            try
            {
                if (hasRole)
                {
                    await guildUser.RemoveRoleAsync(item.RoleId);
                    heldCount--;
                    results.Add($"✖ Removed **{item.Label}**");
                }
                else if (panel.MaxRoles > 0 && heldCount >= panel.MaxRoles)
                {
                    results.Add($"⚠ **{item.Label}** skipped — limit is {panel.MaxRoles} role(s) from this panel. Remove one first.");
                }
                else
                {
                    await guildUser.AddRoleAsync(item.RoleId);
                    heldCount++;
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

        var guildUser = interaction.User as IGuildUser;
        if (guildUser is null && interaction.Channel is IGuildChannel guildChannel)
        {
            guildUser = await guildChannel.Guild.GetUserAsync(interaction.User.Id);
        }

        if (guildUser is null)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Error",
                    "Could not resolve your member profile on this server. Please try again or contact an admin."),
                ephemeral: true);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var panelService = scope.ServiceProvider.GetRequiredService<IRolePanelService>();

        // Load panel kèm items — cần để enforce MaxRoles
        var panel = await panelService.GetPanelByItemAsync(itemId);
        var item = panel?.Items.FirstOrDefault(i => i.Id == itemId);
        if (panel is null || item is null)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Not Found",
                    "This role no longer exists in the panel database (likely due to a database migration/reset or deletion). Please ask an admin to recreate this panel using `/rolepanel create`."),
                ephemeral: true);
            return;
        }

        var role = guildUser.Guild.GetRole(item.RoleId);
        if (role is null)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Role Deleted",
                    $"The role **{item.Label}** no longer exists on this server. Please ask an admin to update the panel."),
                ephemeral: true);
            return;
        }

        var socketGuild = ((SocketGuildChannel)interaction.Channel).Guild;
        var botUser = socketGuild.CurrentUser;
        if (botUser is not null && botUser.Hierarchy <= role.Position)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Permission Error",
                    $"My bot role is below **@{role.Name}** in Server Settings → Roles. Please ask an admin to drag the bot's role higher!"),
                ephemeral: true);
            return;
        }

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
                // Enforce MaxRoles: đang giữ đủ số role từ panel này → từ chối
                if (panel.MaxRoles > 0)
                {
                    var heldCount = panel.Items.Count(i => guildUser.RoleIds.Contains(i.RoleId));
                    if (heldCount >= panel.MaxRoles)
                    {
                        await interaction.FollowupAsync(
                            embed: RoleManagementEmbeds.Warn("Limit Reached",
                                $"You can only hold **{panel.MaxRoles}** role(s) from this panel. " +
                                "Remove one first by clicking its button."),
                            ephemeral: true);
                        return;
                    }
                }

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
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == Discord.DiscordErrorCode.MissingPermissions)
        {
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Missing Permissions",
                    $"The bot does not have permission (`Manage Roles`) or proper hierarchy to assign **{item.Label}**. Please ask an admin to fix bot roles."),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", item.RoleId, guildUser.Id);
            await interaction.FollowupAsync(
                embed: RoleManagementEmbeds.Error("Error",
                    $"Failed to update role: {ex.Message}. Please contact an admin."),
                ephemeral: true);
        }
    }
}