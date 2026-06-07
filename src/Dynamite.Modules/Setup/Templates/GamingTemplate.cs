// src/Dynamite.Modules/Setup/Templates/GamingTemplate.cs
namespace Dynamite.Modules.Setup.Templates;

using Discord;

// Gaming Server Template
// ─────────────────────
// Structure:
//   Roles: Admin, Moderator, Member, Bot
//   Categories:
//     📋 INFORMATION  — rules, announcements (read-only for members)
//     💬 GENERAL      — general chat, memes, off-topic
//     🎮 GAMING       — looking-for-game, clips, game-specific
//     🔊 VOICE        — General VC, Gaming VC, AFK
//     📊 STAFF        — mod-chat (hidden from members)
//
// Permission philosophy:
//   - @everyone can READ general channels but cannot SEND in info channels
//   - Staff channels are invisible to @everyone

public static class GamingTemplate
{
    public static SetupTemplate Create() => new()
    {
        Name = "Gaming",
        Description = "A gaming community server with info, general, gaming, and voice channels.",

        Roles =
        [
            new() { Name = "Admin",     Color = new Color(0xED4245), Hoisted = true,  Permissions = GuildPermission.Administrator },
            new() { Name = "Moderator", Color = new Color(0xFEE75C), Hoisted = true,  Permissions = GuildPermission.KickMembers | GuildPermission.BanMembers | GuildPermission.ManageMessages | GuildPermission.MuteMembers },
            new() { Name = "Member",    Color = new Color(0x57F287), Hoisted = false, Permissions = null },
            new() { Name = "Bot",       Color = new Color(0x5865F2), Hoisted = true,  Permissions = null },
        ],

        Categories =
        [
            new()
            {
                Name = "📋 INFORMATION",
                Overwrites =
                [
                    new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny, addReactions: PermValue.Deny) }
                ],
                Channels =
                [
                    new() { Name = "📜rules",          Topic = "Server rules — please read before chatting." },
                    new() { Name = "📣announcements",  Topic = "Important server announcements." },
                    new() { Name = "👋welcome",        Topic = "Welcome to the server!" },
                ]
            },

            new()
            {
                Name = "💬 GENERAL",
                Channels =
                [
                    new() { Name = "💬general",      Topic = "General chat for anything." },
                    new() { Name = "😄memes",        Topic = "Memes and funny content." },
                    new() { Name = "🔗off-topic",    Topic = "Anything that doesn't fit elsewhere." },
                    new() { Name = "🤖bot-commands", Topic = "Use bot commands here." },
                ]
            },

            new()
            {
                Name = "🎮 GAMING",
                Channels =
                [
                    new() { Name = "🎮looking-for-game",  Topic = "Find teammates! Mention the game you're playing." },
                    new() { Name = "🎬clips-and-montages", Topic = "Share your best plays." },
                    new() { Name = "💰game-deals",         Topic = "Share sales and free game offers." },
                ]
            },

            new()
            {
                Name = "🔊 VOICE",
                Channels =
                [
                    new() { Name = "General",  Type = ChannelType.Voice },
                    new() { Name = "Gaming #1", Type = ChannelType.Voice },
                    new() { Name = "Gaming #2", Type = ChannelType.Voice },
                    new() { Name = "AFK",       Type = ChannelType.Voice },
                ]
            },

            new()
            {
                Name = "📊 STAFF",
                Overwrites =
                [
                    // @everyone không thấy category này
                    new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Deny) },
                    // Moderator có thể thấy và chat
                    new() { TargetRoleName = "Moderator", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow) },
                    new() { TargetRoleName = "Admin", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow) },
                ],
                Channels =
                [
                    new() { Name = "🔧mod-chat",   Topic = "Staff only discussion." },
                    new() { Name = "📋mod-log",    Topic = "Moderation action log." },
                    new() { Name = "🚨reports",    Topic = "User reports." },
                ]
            },
        ]
    };
}