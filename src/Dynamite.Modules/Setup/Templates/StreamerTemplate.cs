// src/Dynamite.Modules/Setup/Templates/StreamerTemplate.cs
namespace Dynamite.Modules.Setup.Templates;

using Discord;

// Streamer Server Template
// ────────────────────────
// Structure:
//   Roles: Admin, Moderator, Subscriber, Follower, Bot
//   Categories:
//     📋 INFORMATION  — rules, schedule, socials (read-only)
//     📣 STREAM       — announcements, clips, stream-chat
//     💬 COMMUNITY    — general, off-topic, fan-art
//     🎙 VOICE        — Hang Out, Watch Party, AFK
//     🔐 STAFF        — hidden staff channels

public static class StreamerTemplate
{
    public static SetupTemplate Create() => new()
    {
        Name = "Streamer",
        Description = "A streamer community server with schedule, stream updates, and fan channels.",

        Roles =
        [
            new() { Name = "Admin",       Color = new Color(0xED4245), Hoisted = true },
            new() { Name = "Moderator",   Color = new Color(0x5865F2), Hoisted = true,
                    Permissions = GuildPermission.KickMembers | GuildPermission.ManageMessages | GuildPermission.MuteMembers },
            new() { Name = "Subscriber",  Color = new Color(0xFEE75C), Hoisted = true,  Mentionable = true },
            new() { Name = "Follower",    Color = new Color(0x57F287), Hoisted = false, Mentionable = false },
            new() { Name = "Bot",         Color = new Color(0x5865F2), Hoisted = true },
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
                    new() { Name = "📜rules",       Topic = "Community rules and expectations." },
                    new() { Name = "📅schedule",    Topic = "Stream schedule — updated weekly." },
                    new() { Name = "🔗socials",     Topic = "Links to all social media." },
                ]
            },

            new()
            {
                Name = "📣 STREAM",
                Channels =
                [
                    new()
                    {
                        Name = "🔴stream-announcements",
                        Topic = "Go-live and stream updates.",
                        // Slow mode để tránh spam khi streamer announce live
                        Overwrites =
                        [
                            new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll
                                .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny, addReactions: PermValue.Allow) }
                        ]
                    },
                    new() { Name = "💬stream-chat",    Topic = "Chat during the stream.", SlowModeInterval = 5 },
                    new() { Name = "🎬clips",          Topic = "Share stream clips and highlights." },
                    new() { Name = "🎁giveaways",      Topic = "Giveaway announcements." },
                ]
            },

            new()
            {
                Name = "💬 COMMUNITY",
                Channels =
                [
                    new() { Name = "💬general",      Topic = "General community chat." },
                    new() { Name = "🎨fan-art",      Topic = "Share fan art and creative content." },
                    new() { Name = "🔗off-topic",    Topic = "Anything off-topic." },
                    new() { Name = "🤖bot-commands", Topic = "Use bot commands here.", SlowModeInterval = 3 },
                ]
            },

            new()
            {
                Name = "🎙 VOICE",
                Channels =
                [
                    new() { Name = "Hang Out",    Type = ChannelType.Voice },
                    new() { Name = "Watch Party", Type = ChannelType.Voice },
                    new() { Name = "AFK",         Type = ChannelType.Voice },
                ]
            },

            new()
            {
                Name = "🔐 STAFF",
                Overwrites =
                [
                    new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Deny) },
                    new() { TargetRoleName = "Moderator", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow) },
                    new() { TargetRoleName = "Admin", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow) },
                ],
                Channels =
                [
                    new() { Name = "🔧mod-chat",   Topic = "Staff coordination." },
                    new() { Name = "📋mod-log",    Topic = "Moderation log." },
                    new() { Name = "🚨reports",    Topic = "Viewer reports." },
                ]
            },
        ]
    };
}