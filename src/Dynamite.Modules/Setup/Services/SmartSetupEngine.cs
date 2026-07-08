// src/Dynamite.Modules/Setup/Services/SmartSetupEngine.cs
namespace Dynamite.Modules.Setup.Services;

using Discord;
using Dynamite.Modules.Setup.Templates;
using Microsoft.Extensions.Logging;

public enum SmartServerTopic
{
    Gaming,
    Study,
    Community,
    CryptoOrTech
}

public enum SmartServerScale
{
    Small,  // < 100 members
    Medium, // 100 - 1000 members
    Large   // > 1000 members
}

public class SmartSetupOptions
{
    public SmartServerTopic Topic { get; set; } = SmartServerTopic.Community;
    public SmartServerScale Scale { get; set; } = SmartServerScale.Medium;
    public bool EnableEconomy { get; set; } = true;
    public bool EnableTicket { get; set; } = true;
    public bool EnableModeration { get; set; } = true;
    public bool EnableVoice { get; set; } = true;
}

public class SmartSetupEngine
{
    private readonly ILogger<SmartSetupEngine> _logger;

    public SmartSetupEngine(ILogger<SmartSetupEngine> logger)
    {
        _logger = logger;
    }

    public SetupTemplate GenerateTemplate(SmartSetupOptions options)
    {
        _logger.LogInformation("Generating Smart Setup template for Topic={Topic}, Scale={Scale}, Economy={E}, Ticket={T}, Mod={M}, Voice={V}",
            options.Topic, options.Scale, options.EnableEconomy, options.EnableTicket, options.EnableModeration, options.EnableVoice);

        var topicName = options.Topic switch
        {
            SmartServerTopic.Gaming => "Esports & Gaming",
            SmartServerTopic.Study => "Study & Research Club",
            SmartServerTopic.CryptoOrTech => "Tech & Crypto Hub",
            _ => "Vibrant Community"
        };

        var roles = new List<RoleTemplate>
        {
            new() { Name = "Admin",     Color = new Color(0xED4245), Hoisted = true, Permissions = GuildPermission.Administrator },
            new() { Name = "Moderator", Color = new Color(0xFEE75C), Hoisted = true, Permissions = GuildPermission.KickMembers | GuildPermission.BanMembers | GuildPermission.ManageMessages | GuildPermission.MuteMembers },
            new() { Name = "VIP / Booster", Color = new Color(0xE67E22), Hoisted = true, Permissions = null },
            new() { Name = "Member",    Color = new Color(0x57F287), Hoisted = false, Permissions = null },
            new() { Name = "Bot",       Color = new Color(0x5865F2), Hoisted = true, Permissions = null }
        };

        var categories = new List<CategoryTemplate>();

        // 1. INFO CATEGORY (Read-only for everyone)
        categories.Add(new CategoryTemplate
        {
            Name = "📋 INFORMATION",
            Overwrites =
            [
                new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll
                    .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny, addReactions: PermValue.Allow) }
            ],
            Channels =
            [
                new() { Name = "welcome", Topic = "Welcome new members to the server!" },
                new() { Name = "rules", Topic = "Server rules and guidelines. Please read carefully." },
                new() { Name = "announcements", Topic = "Official news and server updates." }
            ]
        });

        // 2. GENERAL / TOPIC CATEGORY
        var generalChannels = new List<ChannelTemplate>
        {
            new() { Name = "general-chat", Topic = "General discussion and hang out space.", SlowModeInterval = options.Scale == SmartServerScale.Large ? 10 : 0 },
            new() { Name = "media-and-memes", Topic = "Share pictures, clips, and memes." }
        };

        switch (options.Topic)
        {
            case SmartServerTopic.Gaming:
                generalChannels.Add(new() { Name = "looking-for-group", Topic = "Find teammates and party up!" });
                generalChannels.Add(new() { Name = "highlights-and-clips", Topic = "Best gaming clips and clutch moments." });
                break;
            case SmartServerTopic.Study:
                generalChannels.Add(new() { Name = "study-resources", Topic = "Books, docs, and learning materials." });
                generalChannels.Add(new() { Name = "q-and-a-help", Topic = "Ask questions and help fellow students." });
                break;
            case SmartServerTopic.CryptoOrTech:
                generalChannels.Add(new() { Name = "market-and-tech-talk", Topic = "Discuss market trends and code." });
                generalChannels.Add(new() { Name = "project-showcase", Topic = "Showcase your amazing projects!" });
                break;
        }

        categories.Add(new CategoryTemplate
        {
            Name = $"💬 {topicName.ToUpperInvariant()}",
            Channels = generalChannels
        });

        // 3. ECONOMY & MINI-GAMES CATEGORY (If enabled)
        if (options.EnableEconomy)
        {
            categories.Add(new CategoryTemplate
            {
                Name = "🎰 ECONOMY & FISHING",
                Channels =
                [
                    new() { Name = "fishing-pond", Topic = "Cast your line and catch rare fish with /fish!" },
                    new() { Name = "casino-and-slots", Topic = "Play economy mini-games and check leaderboards." },
                    new() { Name = "shop-and-market", Topic = "Buy bait and items with your coins." }
                ]
            });
        }

        // 4. VOICE CATEGORY (If enabled)
        if (options.EnableVoice)
        {
            var voiceChannels = new List<ChannelTemplate>
            {
                new() { Name = "➕ Join to Create", Type = ChannelType.Voice, IsTempVoiceTrigger = true },
                new() { Name = "General Lounge", Type = ChannelType.Voice },
                new() { Name = "Chill & Music", Type = ChannelType.Voice }
            };

            if (options.Topic == SmartServerTopic.Gaming)
            {
                voiceChannels.Add(new() { Name = "Squad 1 (Limit 5)", Type = ChannelType.Voice });
                voiceChannels.Add(new() { Name = "Squad 2 (Limit 5)", Type = ChannelType.Voice });
            }
            else if (options.Topic == SmartServerTopic.Study)
            {
                voiceChannels.Add(new() { Name = "Quiet Study Room", Type = ChannelType.Voice });
            }

            categories.Add(new CategoryTemplate
            {
                Name = "🔊 VOICE CHANNELS",
                Channels = voiceChannels
            });
        }

        // 5. SUPPORT & TICKETS CATEGORY (If enabled)
        if (options.EnableTicket)
        {
            categories.Add(new CategoryTemplate
            {
                Name = "📩 SUPPORT & TICKETS",
                Overwrites =
                [
                    new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll
                        .Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny) }
                ],
                Channels =
                [
                    new() { Name = "open-a-ticket", Topic = "Click the button below to open a private support ticket with moderators." }
                ]
            });
        }

        // 6. STAFF CATEGORY (Hidden from @everyone)
        if (options.EnableModeration)
        {
            categories.Add(new CategoryTemplate
            {
                Name = "🛡️ STAFF ONLY",
                Overwrites =
                [
                    new() { TargetRoleName = "@everyone", Permissions = OverwritePermissions.InheritAll.Modify(viewChannel: PermValue.Deny) },
                    new() { TargetRoleName = "Moderator", Permissions = OverwritePermissions.InheritAll.Modify(viewChannel: PermValue.Allow) },
                    new() { TargetRoleName = "Admin",     Permissions = OverwritePermissions.InheritAll.Modify(viewChannel: PermValue.Allow) }
                ],
                Channels =
                [
                    new() { Name = "mod-chat", Topic = "Staff coordination and discussion." },
                    new() { Name = "mod-logs", Topic = "Automated moderation and action logs." }
                ]
            });
        }

        return new SetupTemplate
        {
            Name = $"Smart Setup ({topicName})",
            Description = $"Automated tailored setup for a {options.Scale} {options.Topic} community.",
            Roles = roles,
            Categories = categories
        };
    }
}
