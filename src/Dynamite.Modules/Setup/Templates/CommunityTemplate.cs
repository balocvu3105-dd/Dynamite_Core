// src/Dynamite.Modules/Setup/Templates/CommunityTemplate.cs
namespace Dynamite.Modules.Setup.Templates;

using Discord;

// Community Server Template
// ─────────────────────────
// Structure:
//   Roles: Admin, Moderator, Member, Verified, Bot
//   Categories:
//     📋 START HERE   — rules, roles, faq (read-only)
//     📣 UPDATES      — announcements, changelog (read-only)
//     💬 COMMUNITY    — general, introductions, media, events
//     🎙 VOICE        — Lounge, Study Hall, Events
//     💼 STAFF        — hidden from members

public static class CommunityTemplate
{
    public static SetupTemplate Create() => new()
    {
        Name = "Community",
        Description = "A community hub with clear onboarding, updates, and discussion channels.",

        Roles =
        [
            new() { Name = "Admin",     Color = new Color(0xED4245), Hoisted = true },
            new() { Name = "Moderator", Color = new Color(0x5865F2), Hoisted = true,
                    Permissions = GuildPermission.KickMembers | GuildPermission.ManageMessages | GuildPermission.MuteMembers },
            new() { Name = "Verified",  Color = new Color(0x57F287), Hoisted = false },
            new() { Name = "Member",    Color = new Color(0xAAAAAA), Hoisted = false },
            new() { Name = "Bot",       Color = new Color(0xFEE75C), Hoisted = true },
        ],

        Categories =
        [
            new()
            {
                Name = "📋 START HERE",
                Overwrites =
                [
                    new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny, addReactions: PermValue.Deny) }
                ],
                Channels =
                [
                    new() { Name = "📜rules",           Topic = "Community rules and guidelines." },
                    new() { Name = "🎭self-roles",      Topic = "Assign yourself roles here." },
                    new() { Name = "❓faq",             Topic = "Frequently asked questions." },
                ]
            },

            new()
            {
                Name = "📣 UPDATES",
                Overwrites =
                [
                    new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny, addReactions: PermValue.Allow) }
                ],
                Channels =
                [
                    new() { Name = "📣announcements",   Topic = "Official announcements." },
                    new() { Name = "📰changelog",       Topic = "What's new." },
                    new() { Name = "📅events",          Topic = "Upcoming events." },
                ]
            },

            new()
            {
                Name = "💬 COMMUNITY",
                Channels =
                [
                    new() { Name = "👋introductions",   Topic = "Introduce yourself to the community." },
                    new() { Name = "💬general",         Topic = "General discussion." },
                    new() { Name = "🖼media",           Topic = "Photos, art, and media." },
                    new() { Name = "🤖bot-commands",    Topic = "Use bot commands here.", SlowModeInterval = 3 },
                ]
            },

            new()
            {
                Name = "🎙 VOICE",
                Channels =
                [
                    new() { Name = "➕ Join to Create", Type = ChannelType.Voice, IsTempVoiceTrigger = true },
                    new() { Name = "Lounge",      Type = ChannelType.Voice },
                    new() { Name = "Study Hall",  Type = ChannelType.Voice },
                    new() { Name = "Events",      Type = ChannelType.Voice },
                    new() { Name = "AFK",         Type = ChannelType.Voice },
                ]
            },

            new()
            {
                Name = "💼 STAFF",
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
                    new() { Name = "🔧mod-chat",    Topic = "Staff discussion." },
                    new() { Name = "📋mod-log",     Topic = "Moderation log." },
                    new() { Name = "🚨reports",     Topic = "Member reports." },
                    new() { Name = "📌staff-notes", Topic = "Pinned staff notes." },
                ]
            },
        ]
    };
}