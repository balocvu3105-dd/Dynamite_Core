namespace Dynamite.Modules.Moderation.Helpers;

using Discord;
using Discord.WebSocket;

public static class HierarchyHelper
{
    public static string? ValidateHierarchy(SocketGuild guild, SocketGuildUser target, SocketGuildUser moderator)
    {
        if (target.Id == moderator.Id)
            return "You cannot moderate yourself.";

        if (target.Id == guild.OwnerId)
            return "You cannot moderate the server owner.";

        var botUser = guild.CurrentUser;

        if (botUser.Hierarchy <= target.Hierarchy)
            return "My role is not high enough to moderate this user.";

        if (guild.OwnerId != moderator.Id && moderator.Hierarchy <= target.Hierarchy)
            return "Your role is not high enough to moderate this user.";

        return null; // null = valid
    }
}
