// src/Dynamite.API/Auth/GuildAuthorizationService.cs
namespace Dynamite.API.Auth;

using Dynamite.API.DTOs.Auth;

/// <summary>
/// Check xem user có quyền manage một guild cụ thể không.
///
/// Tại sao không inject DiscordSocketClient vào đây?
/// → Dynamite.API là một process riêng biệt với Dynamite.Bot.
/// → API không có access vào Discord gateway connection.
/// → Thay vào đó, permission check dựa hoàn toàn vào data
///   đã fetch từ Discord API lúc login (guilds list + permission bitmask).
/// </summary>
public class GuildAuthorizationService
{
    // ManageGuild permission bit theo Discord docs
    private const long ManageGuildPermission = 0x20;

    /// <summary>
    /// Check user có ManageGuild permission trong guild này không.
    /// Permissions là bitmask lấy từ /users/@me/guilds response.
    /// </summary>
    public bool UserCanManageGuild(long permissions)
        => (permissions & ManageGuildPermission) != 0;

    /// <summary>
    /// Từ list guilds của user, tìm guild có id khớp.
    /// Trả về null nếu user không có guild đó hoặc không có quyền.
    /// </summary>
    public DiscordGuildDto? GetManageableGuild(
        IEnumerable<DiscordGuildDto> userGuilds,
        string guildId)
        => userGuilds.FirstOrDefault(g =>
            g.Id == guildId &&
            UserCanManageGuild(g.Permissions));
}