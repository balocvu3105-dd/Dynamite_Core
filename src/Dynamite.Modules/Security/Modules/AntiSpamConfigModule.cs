// src/Dynamite.Modules/Security/Modules/AntiSpamConfigModule.cs
namespace Dynamite.Modules.Security.Modules;

using Discord;
using Discord.Interactions;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Security.Helpers;
using Microsoft.Extensions.Logging;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
[Group("antispam", "Configure the anti-spam and security system")]
public class AntiSpamConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAntiSpamService _antiSpamService;
    private readonly ILogger<AntiSpamConfigModule> _logger;

    public AntiSpamConfigModule(IAntiSpamService antiSpamService, ILogger<AntiSpamConfigModule> logger)
    {
        _antiSpamService = antiSpamService;
        _logger = logger;
    }

    [SlashCommand("enable", "Enable or disable the anti-spam system")]
    public async Task EnableAsync(
        [Summary("enabled", "Enable or disable")] bool enabled)
    {
        await DeferAsync(ephemeral: true);
        await _antiSpamService.SetEnabledAsync(Context.Guild.Id, Context.Guild.Name, enabled);

        await FollowupAsync(embed: SecurityEmbeds.Success(
            $"AntiSpam {(enabled ? "Enabled" : "Disabled")}",
            $"The anti-spam system is now **{(enabled ? "active" : "inactive")}**."),
            ephemeral: true);
    }

    [SlashCommand("config", "Set spam detection thresholds")]
    public async Task ConfigAsync(
        [Summary("messages", "Max messages allowed in the time window (2-30)")] int messages,
        [Summary("seconds", "Time window in seconds (2-60)")] int seconds)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            await _antiSpamService.SetMessageThresholdAsync(
                Context.Guild.Id, Context.Guild.Name, messages, seconds);

            await FollowupAsync(embed: SecurityEmbeds.Success(
                "Spam Threshold Updated",
                $"Spam will be detected if a user sends **{messages}** messages in **{seconds}** seconds."),
                ephemeral: true);
        }
        catch (ArgumentException ex)
        {
            await FollowupAsync(embed: SecurityEmbeds.Error("Invalid Value", ex.Message),
                ephemeral: true);
        }
    }

    [SlashCommand("mentions", "Set max allowed mentions per message")]
    public async Task MentionsAsync(
        [Summary("threshold", "Max mentions before action (2-20)")] int threshold)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            await _antiSpamService.SetMentionThresholdAsync(
                Context.Guild.Id, Context.Guild.Name, threshold);

            await FollowupAsync(embed: SecurityEmbeds.Success(
                "Mention Threshold Updated",
                $"Messages with **{threshold}+** mentions will be flagged as spam."),
                ephemeral: true);
        }
        catch (ArgumentException ex)
        {
            await FollowupAsync(embed: SecurityEmbeds.Error("Invalid Value", ex.Message),
                ephemeral: true);
        }
    }

    [SlashCommand("antiinvite", "Block Discord invite links")]
    public async Task AntiInviteAsync(
        [Summary("enabled", "Enable or disable")] bool enabled)
    {
        await DeferAsync(ephemeral: true);
        await _antiSpamService.SetFeatureAsync(
            Context.Guild.Id, Context.Guild.Name, "antiinvite", enabled);

        await FollowupAsync(embed: SecurityEmbeds.Success(
            $"Anti-Invite {(enabled ? "Enabled" : "Disabled")}",
            $"Discord invite links are now {(enabled ? "blocked" : "allowed")}."),
            ephemeral: true);
    }

    [SlashCommand("antiscam", "Block known scam links")]
    public async Task AntiScamAsync(
        [Summary("enabled", "Enable or disable")] bool enabled)
    {
        await DeferAsync(ephemeral: true);
        await _antiSpamService.SetFeatureAsync(
            Context.Guild.Id, Context.Guild.Name, "antiscam", enabled);

        await FollowupAsync(embed: SecurityEmbeds.Success(
            $"Anti-Scam {(enabled ? "Enabled" : "Disabled")}",
            $"Scam link detection is now {(enabled ? "active" : "inactive")}."),
            ephemeral: true);
    }

    [SlashCommand("antiraid", "Enable raid detection and mod alerts")]
    public async Task AntiRaidAsync(
        [Summary("enabled", "Enable or disable")] bool enabled,
        [Summary("threshold", "Joins per 10 seconds to trigger alert (3-50)")] int threshold = 10)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            await _antiSpamService.SetFeatureAsync(
                Context.Guild.Id, Context.Guild.Name, "antiraid", enabled);

            if (enabled)
                await _antiSpamService.SetRaidThresholdAsync(
                    Context.Guild.Id, Context.Guild.Name, threshold);

            await FollowupAsync(embed: SecurityEmbeds.Success(
                $"Anti-Raid {(enabled ? "Enabled" : "Disabled")}",
                enabled
                    ? $"Raid alerts will trigger when **{threshold}+** users join in 10 seconds."
                    : "Raid detection is now disabled."),
                ephemeral: true);
        }
        catch (ArgumentException ex)
        {
            await FollowupAsync(embed: SecurityEmbeds.Error("Invalid Value", ex.Message),
                ephemeral: true);
        }
    }

    [SlashCommand("view", "View current anti-spam configuration")]
    public async Task ViewAsync()
    {
        await DeferAsync(ephemeral: true);

        var config = await _antiSpamService.GetConfigAsync(Context.Guild.Id);

        if (config is null)
        {
            await FollowupAsync(embed: SecurityEmbeds.Info(
                "Not Configured",
                "AntiSpam has not been set up yet. Use `/antispam enable true` to start."),
                ephemeral: true);
            return;
        }

        static string Bool(bool v) => v ? "✅ On" : "❌ Off";

        var embed = new EmbedBuilder()
            .WithTitle("🛡️ AntiSpam Configuration")
            .WithColor(new Color(0x5865F2))
            .AddField("Status", Bool(config.Enabled), inline: true)
            .AddField("Anti-Invite", Bool(config.AntiInvite), inline: true)
            .AddField("Anti-Scam", Bool(config.AntiScamLink), inline: true)
            .AddField("Anti-Raid", Bool(config.AntiRaid), inline: true)
            .AddField("Spam Threshold",
                $"{config.MessageThreshold} msgs / {config.MessageWindowSeconds}s", inline: true)
            .AddField("Mention Limit", $"{config.MentionThreshold} mentions", inline: true)
            .AddField("Raid Threshold", $"{config.RaidThreshold} joins / 10s", inline: true)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }
}