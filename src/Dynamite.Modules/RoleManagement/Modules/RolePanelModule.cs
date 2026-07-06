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
[RequireUserPermission(GuildPermission.Administrator)]
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
        [Summary("channel", "Channel to post the panel in")] ITextChannel channel,
        [Summary("max_roles", "Max roles a member can hold from this panel (0 = unlimited)")]
        [MinValue(0)] [MaxValue(25)] int maxRoles = 0)
    {
        // maxRoles truyền qua custom_id vì modal không có chỗ chứa state
        var customId = $"rolepanel_create_{(int)panelType}_{channel.Id}_{maxRoles}";
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

    [SlashCommand("add-role", "Thêm một role vào panel đã tồn tại")]
    public async Task AddRoleAsync(
        [Summary("panel_id", "ID của panel (lấy từ /rolepanel list)")] string panelIdStr,
        [Summary("role",     "Role muốn thêm")]                          IRole role,
        [Summary("label",    "Nhãn hiển thị trên nút (để trống = tên role)")] string? label = null,
        [Summary("emoji",    "Emoji hiển thị trên nút")]                  string? emoji = null)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(panelIdStr, out var panelId))
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Invalid ID", "Panel ID không hợp lệ."), ephemeral: true);
            return;
        }

        // Validate role
        var botTopRole = Context.Guild.CurrentUser.Roles.Max(r => r.Position);
        if (role.Position >= botTopRole)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Invalid Role", "Role này cao hơn role của bot — không thể assign."), ephemeral: true);
            return;
        }
        if (role.IsManaged)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Invalid Role", "Managed role (bot/integration) không thể assign."), ephemeral: true);
            return;
        }

        var finalLabel = string.IsNullOrWhiteSpace(label) ? role.Name : label;
        var dto = new RolePanelItemDto(role.Id, finalLabel, emoji, null);

        var result = await _panelService.AddItemAsync(panelId, dto);
        if (!result)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error("Failed", result.ErrorMessage), ephemeral: true);
            return;
        }

        var updatedPanel = result.Value!;

        // Edit message Discord để cập nhật component
        var channel = Context.Guild.GetTextChannel(updatedPanel.ChannelId);
        if (channel is not null)
        {
            try
            {
                var (embed, component) = _panelBuilder.Build(updatedPanel);
                await channel.ModifyMessageAsync(updatedPanel.MessageId, m =>
                {
                    m.Embed      = embed;
                    m.Components = component;
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not edit panel message {MessageId}", updatedPanel.MessageId);
            }
        }

        await FollowupAsync(embed: RoleManagementEmbeds.Success(
            "Role Added",
            $"Đã thêm {role.Mention} vào panel **{updatedPanel.Title}**."), ephemeral: true);
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

    [ModalInteraction("rolepanel_create_*_*_*")]
    public async Task OnRolePanelModalAsync(
        string panelTypeStr, string channelIdStr, string maxRolesStr, RolePanelCreateModal modal)
    {
        await DeferAsync(ephemeral: true);

        if (!int.TryParse(maxRolesStr, out var maxRoles) || maxRoles < 0)
            maxRoles = 0;

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
                "Could not parse any roles.\nFormat (one per line): `RoleID` hoặc `RoleID Label Emoji`\nRole mention `<@&id>` cũng được chấp nhận.\nExample:\n```\n1234567890\n1234567890 Gaming 🎮\n```"),
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

        // Validate role IDs NGAY khi tạo panel — tránh lưu ID rác vào DB
        // rồi user bấm nút mới phát hiện role không tồn tại
        var botTopRole = Context.Guild.CurrentUser.Roles.Max(r => r.Position);
        var invalid = new List<string>();
        var validItems = new List<(ulong RoleId, string Label, string? Emoji)>();
        foreach (var (roleId, label, emoji) in items)
        {
            var role = Context.Guild.GetRole(roleId);
            if (role is null)
                invalid.Add($"`{roleId}` ({label}) — role not found on this server");
            else if (role.Position >= botTopRole)
                invalid.Add($"`{roleId}` ({label}) — role is higher than the bot's top role, cannot be assigned");
            else if (role.IsManaged)
                invalid.Add($"`{roleId}` ({label}) — managed role (bot/integration), cannot be assigned");
            else
                // Label trống → dùng tên role thật
                validItems.Add((roleId, string.IsNullOrWhiteSpace(label) ? role.Name : label, emoji));
        }

        if (invalid.Count > 0)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Invalid Roles",
                "These roles cannot be used:\n" + string.Join("\n", invalid) +
                "\n\nTip: enable Developer Mode → right-click the role → Copy Role ID."),
                ephemeral: true);
            return;
        }

        // FIX: Persist vào DB trước để lấy Guid thật của items,
        // sau đó dùng Guid đó để build button custom_id.
        // Nếu build trước → Guid trong button khác Guid trong DB → lookup fail.
        var rolePanelItems = validItems.Select(i => new RolePanelItemDto(i.RoleId, i.Label, i.Emoji, null));
        var savedPanel = await _panelService.CreatePanelAsync(
            Context.Guild.Id,
            Context.Guild.Name,
            channelId,
            0, // placeholder messageId — sẽ update sau khi post
            modal.PanelTitle,
            string.IsNullOrWhiteSpace(modal.Description) ? null : modal.Description,
            panelType,
            rolePanelItems,
            maxRoles);

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
            $"Role panel **{modal.PanelTitle}** posted in {channel.Mention} with {validItems.Count} role(s)."),
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

            // Chấp nhận cả role mention `<@&123>` lẫn ID thuần `123`
            var idToken = parts[0];
            if (idToken.StartsWith("<@&") && idToken.EndsWith(">"))
                idToken = idToken[3..^1];

            if (!ulong.TryParse(idToken, out var roleId)) continue;

            // Format: `ID [Label có thể nhiều từ] [Emoji ở cuối]`
            // Token cuối là emoji nếu nó là custom emote <:n:id> hoặc unicode emoji ngắn;
            // phần còn lại sau ID là label (hỗ trợ khoảng trắng).
            string? emoji = null;
            var labelEnd = parts.Length;

            if (parts.Length >= 2 && IsEmojiToken(parts[^1]))
            {
                emoji = parts[^1];
                labelEnd = parts.Length - 1;
            }

            var label = labelEnd > 1
                ? string.Join(' ', parts[1..labelEnd])
                : string.Empty; // trống → bước validate sẽ thay bằng tên role thật

            result.Add((roleId, label, emoji));
        }

        return result;
    }

    // Token được coi là emoji nếu: custom emote `<:name:id>` / `<a:name:id>`,
    // hoặc chuỗi ngắn không chứa chữ/số (unicode emoji thuộc nhóm Symbol).
    // Label tiếng Việt ("Hưởng"...) chứa chữ cái unicode nên không bị nhận nhầm.
    private static bool IsEmojiToken(string token)
    {
        if (token.StartsWith("<:") || token.StartsWith("<a:"))
            return token.EndsWith(">");

        return token.Length <= 8 && !token.Any(char.IsLetterOrDigit);
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