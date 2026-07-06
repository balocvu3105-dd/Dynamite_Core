// src/Dynamite.API/Auth/GuildAuthorizationService.cs
namespace Dynamite.API.Auth;

using Dynamite.API.DTOs.Auth;

/// <summary>
/// Check xem user có quyền manage (Admin hoặc Owner) một guild cụ thể không.
///
/// Tại sao không inject DiscordSocketClient vào đây?
/// → Dynamite.API là một process riêng biệt với Dynamite.Bot.
/// → API không có access vào Discord gateway connection.
/// → Thay vào đó, permission check dựa hoàn toàn vào data
///   đã fetch từ Discord API lúc login (guilds list + permission bitmask + owner flag).
/// </summary>
public class GuildAuthorizationService
{
    // Administrator permission bit theo Discord docs (0x8)
    private const long AdministratorPermission = 0x8;

    /// <summary>
    /// Check user có Administrator permission hoặc là Owner trong guild này không.
    /// Permissions là bitmask lấy từ /users/@me/guilds response.
    /// </summary>
    public bool UserCanManageGuild(long permissions, bool isOwner = false)
        => isOwner || (permissions & AdministratorPermission) != 0;

    /// <summary>
    /// Từ list guilds của user, tìm guild có id khớp.
    /// Trả về null nếu user không có guild đó hoặc không có quyền (Admin / Owner).
    /// </summary>
    public DiscordGuildDto? GetManageableGuild(
        IEnumerable<DiscordGuildDto> userGuilds,
        string guildId)
        => userGuilds.FirstOrDefault(g =>
            g.Id == guildId &&
            UserCanManageGuild(g.Permissions, g.Owner));
}