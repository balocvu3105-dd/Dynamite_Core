// src/Dynamite.Modules/RoleManagement/Modules/RolePanelModule.cs
namespace Dynamite.Modules.RoleManagement.Modules;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Dynamite.Modules.RoleManagement.Helpers;
using Dynamite.Modules.RoleManagement.Services;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.ManageRoles)]
[Group("rolepanel", "Manage self-assignable role panels")]
public class RolePanelModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IRolePanelService _panelService;
    private readonly RolePanelBuilder _panelBuilder;

    public RolePanelModule(IRolePanelService panelService, RolePanelBuilder panelBuilder)
    {
        _panelService = panelService;
        _panelBuilder = panelBuilder;
    }

    [SlashCommand("create", "Create a new role selection panel")]
    public async Task CreateAsync(
        [Summary("title", "Panel title")] string title,
        [Summary("type", "Button or Select Menu")] RolePanelType panelType,
        [Summary("channel", "Channel to post the panel in")] ITextChannel channel,
        [Summary("description", "Optional description")] string? description = null)
    {
        // Respond with a modal để admin nhập roles
        // Modal nhận role IDs dạng text vì Discord chưa support multi-role picker
        await RespondWithModalAsync<RolePanelModal>($"rolepanel_create:{panelType}:{channel.Id}:{Uri.EscapeDataString(title)}:{Uri.EscapeDataString(description ?? "")}");
    }

    [SlashCommand("delete", "Delete a role panel")]
    public async Task DeleteAsync(
        [Summary("panel_id", "The panel ID from /rolepanel list")] string panelIdStr)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(panelIdStr, out var panelId))
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Invalid ID", "That doesn't look like a valid panel ID."), ephemeral: true);
            return;
        }

        var panels = await _panelService.GetPanelsAsync(Context.Guild.Id);
        var panel = panels.FirstOrDefault(p => p.Id == panelId);

        if (panel is null)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Not Found", "Panel not found in this server."), ephemeral: true);
            return;
        }

        // Try delete the Discord message first
        try
        {
            var ch = Context.Guild.GetTextChannel(panel.ChannelId);
            if (ch is not null)
            {
                try { await ch.DeleteMessageAsync(panel.MessageId); }
                catch { /* message already deleted */ }
            }
        }
        catch
        {
            // Message already gone — that's fine, still delete from DB
        }

        await _panelService.DeletePanelAsync(panelId);

        await FollowupAsync(embed: RoleManagementEmbeds.Success(
            "Panel Deleted", "The role panel has been removed."), ephemeral: true);
    }

    [SlashCommand("list", "List all role panels in this server")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);

        var panels = (await _panelService.GetPanelsAsync(Context.Guild.Id)).ToList();

        if (panels.Count == 0)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Info(
                "No Panels", "No role panels found. Use `/rolepanel create` to create one."), ephemeral: true);
            return;
        }

        var lines = panels.Select(p =>
            $"**{p.Title}** — `{p.Id}`\n" +
            $"  Type: {p.PanelType} | Roles: {p.Items.Count} | <#{p.ChannelId}>");

        await FollowupAsync(embed: RoleManagementEmbeds.Info(
            $"Role Panels ({panels.Count})",
            string.Join("\n\n", lines)), ephemeral: true);
    }
}

// Modal để admin nhập role IDs
// Tại sao Modal? Discord chưa có multi-role picker trong slash command params
// Modal cho phép nhập text area thoải mái hơn
public class RolePanelModal : IModal
{
    public string Title => "Configure Role Panel";

    [InputLabel("Role IDs (one per line, format: RoleID Label Emoji)")]
    [ModalTextInput("roles_input", TextInputStyle.Paragraph,
        placeholder: "1234567890 Gaming 🎮\n9876543210 Music 🎵",
        maxLength: 1000)]
    public string RolesInput { get; set; } = string.Empty;
}