// src/Dynamite.Modules/RoleManagement/Modules/RolePanelModule.cs
namespace Dynamite.Modules.RoleManagement.Modules;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Dynamite.Modules.RoleManagement.Helpers;
using Dynamite.Modules.RoleManagement.Services;
using Microsoft.Extensions.Logging;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.ManageRoles)]
[Group("rolepanel", "Manage self-assignable role panels")]
public class RolePanelModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IRolePanelService _panelService;
    private readonly RolePanelBuilder _panelBuilder;
    private readonly ILogger<RolePanelModule> _logger;

    public RolePanelModule(
        IRolePanelService panelService,
        RolePanelBuilder panelBuilder,
        ILogger<RolePanelModule> logger)
    {
        _panelService = panelService;
        _panelBuilder = panelBuilder;
        _logger = logger;
    }

    [SlashCommand("create", "Create a new role selection panel")]
    public async Task CreateAsync(
        [Summary("type", "Button or Select Menu")] RolePanelType panelType,
        [Summary("channel", "Channel to post the panel in")] ITextChannel channel)
    {
        var customId = $"rolepanel_create_{(int)panelType}_{channel.Id}";
        await RespondWithModalAsync<RolePanelCreateModal>(customId);
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

        try
        {
            var ch = Context.Guild.GetTextChannel(panel.ChannelId);
            if (ch is not null)
            {
                try { await ch.DeleteMessageAsync(panel.MessageId); }
                catch { /* message already deleted — ignore */ }
            }
        }
        catch { /* channel gone — still delete from DB */ }

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

// Modal handler tách riêng khỏi [Group] vì Discord.Net prefix tất cả interactions
// bên trong group module — modal handler phải nằm ngoài group để match đúng custom_id.
public class RolePanelModalModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IRolePanelService _panelService;
    private readonly RolePanelBuilder _panelBuilder;
    private readonly ILogger<RolePanelModalModule> _logger;

    public RolePanelModalModule(
        IRolePanelService panelService,
        RolePanelBuilder panelBuilder,
        ILogger<RolePanelModalModule> logger)
    {
        _panelService = panelService;
        _panelBuilder = panelBuilder;
        _logger = logger;
    }

    [ModalInteraction("rolepanel_create_*_*")]
    public async Task OnRolePanelModalAsync(string panelTypeStr, string channelIdStr, RolePanelCreateModal modal)
    {
        await DeferAsync(ephemeral: true);

        if (!int.TryParse(panelTypeStr, out var panelTypeInt)
            || !Enum.IsDefined(typeof(RolePanelType), panelTypeInt))
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Error", "Invalid panel type. Please try again."), ephemeral: true);
            return;
        }

        if (!ulong.TryParse(channelIdStr, out var channelId))
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Error", "Invalid channel. Please try again."), ephemeral: true);
            return;
        }

        var panelType = (RolePanelType)panelTypeInt;

        var items = ParseRolesInput(modal.RolesInput);
        if (items.Count == 0)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "No Roles",
                "Could not parse any roles.\nFormat: `RoleID Label` (one per line)\nExample:\n```\n1234567890 Gaming 🎮\n```"),
                ephemeral: true);
            return;
        }

        var channel = Context.Guild.GetTextChannel(channelId);
        if (channel is null)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Channel Not Found", "The target channel no longer exists."), ephemeral: true);
            return;
        }

        // FIX: Persist vào DB trước để lấy Guid thật của items,
        // sau đó dùng Guid đó để build button custom_id.
        // Nếu build trước → Guid trong button khác Guid trong DB → lookup fail.
        var rolePanelItems = items.Select(i => new RolePanelItemDto(i.RoleId, i.Label, i.Emoji, null));
        var savedPanel = await _panelService.CreatePanelAsync(
            Context.Guild.Id,
            Context.Guild.Name,
            channelId,
            0, // placeholder messageId — sẽ update sau khi post
            modal.PanelTitle,
            string.IsNullOrWhiteSpace(modal.Description) ? null : modal.Description,
            panelType,
            rolePanelItems);

        // Build component từ savedPanel — Guid của items khớp với DB
        var (embed, component) = _panelBuilder.Build(savedPanel);

        IUserMessage message;
        try
        {
            message = await channel.SendMessageAsync(embed: embed, components: component);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post role panel to channel {ChannelId}", channelId);
            // Rollback DB record nếu post thất bại
            await _panelService.DeletePanelAsync(savedPanel.Id);
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Failed to Post",
                "Could not send the panel to that channel. Check my permissions."), ephemeral: true);
            return;
        }

        // Update messageId thật vào DB
        await _panelService.UpdateMessageIdAsync(savedPanel.Id, message.Id);

        await FollowupAsync(embed: RoleManagementEmbeds.Success(
            "Panel Created",
            $"Role panel **{modal.PanelTitle}** posted in {channel.Mention} with {items.Count} role(s)."),
            ephemeral: true);
    }

    private static List<(ulong RoleId, string Label, string? Emoji)> ParseRolesInput(string input)
    {
        var result = new List<(ulong, string, string?)>();

        foreach (var rawLine in input.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            if (!ulong.TryParse(parts[0], out var roleId)) continue;

            var label = parts[1];
            var emoji = parts.Length >= 3 ? parts[2] : null;

            result.Add((roleId, label, emoji));
        }

        return result;
    }
}

// Modal nhận title, description, và danh sách roles từ admin.
public class RolePanelCreateModal : IModal
{
    public string Title => "Create Role Panel";

    [InputLabel("Panel Title")]
    [ModalTextInput("panel_title", TextInputStyle.Short,
        placeholder: "Choose Your Roles",
        maxLength: 256)]
    public string PanelTitle { get; set; } = string.Empty;

    [InputLabel("Description (optional)")]
    [ModalTextInput("panel_description", TextInputStyle.Short,
        placeholder: "Click to toggle a role",
        maxLength: 200,
        initValue: "")]
    public string Description { get; set; } = string.Empty;

    [InputLabel("Roles (RoleID Label Emoji — one per line)")]
    [ModalTextInput("roles_input", TextInputStyle.Paragraph,
        placeholder: "1234567890 Gaming 🎮\n9876543210 Music 🎵",
        maxLength: 1000)]
    public string RolesInput { get; set; } = string.Empty;
}