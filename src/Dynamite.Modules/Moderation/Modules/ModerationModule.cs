namespace Dynamite.Modules.Moderation.Modules;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Moderation.Helpers;
using Microsoft.Extensions.Logging;

[RequireContext(ContextType.Guild)]
public class ModerationModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IModerationService _modService;
    private readonly ILogger<ModerationModule> _logger;

    public ModerationModule(IModerationService modService, ILogger<ModerationModule> logger)
    {
        _modService = modService;
        _logger = logger;
    }

    [SlashCommand("warn", "Warn a user")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task WarnAsync(
        [Summary("user", "The user to warn")] SocketGuildUser target,
        [Summary("reason", "Reason for the warning")] string reason)
    {
        await DeferAsync(ephemeral: true);

        var guild = (SocketGuild)Context.Guild;
        var moderator = (SocketGuildUser)Context.User;

        var hierarchyError = HierarchyHelper.ValidateHierarchy(guild, target, moderator);
        if (hierarchyError is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Cannot Warn", hierarchyError), ephemeral: true);
            return;
        }

        try
        {
            await _modService.WarnAsync(guild.Id, guild.Name, target.Id, moderator.Id, reason);

            await FollowupAsync(embed: EmbedHelper.ModerationAction(
                "User Warned",
                target.Mention,
                moderator.Mention,
                reason), ephemeral: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warning user {UserId}", target.Id);
            await FollowupAsync(embed: EmbedHelper.Error("Error", "An unexpected error occurred."), ephemeral: true);
        }
    }

    [SlashCommand("kick", "Kick a user from the server")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    [RequireBotPermission(GuildPermission.KickMembers)]
    public async Task KickAsync(
        [Summary("user", "The user to kick")] SocketGuildUser target,
        [Summary("reason", "Reason for the kick")] string reason = "No reason provided")
    {
        await DeferAsync(ephemeral: true);

        var guild = (SocketGuild)Context.Guild;
        var moderator = (SocketGuildUser)Context.User;

        var hierarchyError = HierarchyHelper.ValidateHierarchy(guild, target, moderator);
        if (hierarchyError is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Cannot Kick", hierarchyError), ephemeral: true);
            return;
        }

        try
        {
            await target.KickAsync(reason);
            await _modService.KickAsync(guild.Id, guild.Name, target.Id, moderator.Id, reason);

            await FollowupAsync(embed: EmbedHelper.ModerationAction(
                "User Kicked",
                target.Username,
                moderator.Mention,
                reason), ephemeral: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error kicking user {UserId}", target.Id);
            await FollowupAsync(embed: EmbedHelper.Error("Error", "An unexpected error occurred."), ephemeral: true);
        }
    }

    [SlashCommand("ban", "Ban a user from the server")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task BanAsync(
        [Summary("user", "The user to ban")] SocketGuildUser target,
        [Summary("reason", "Reason for the ban")] string reason = "No reason provided",
        [Summary("delete_messages", "Days of messages to delete (0-7)")] int deleteDays = 0)
    {
        await DeferAsync(ephemeral: true);

        var guild = (SocketGuild)Context.Guild;
        var moderator = (SocketGuildUser)Context.User;

        var hierarchyError = HierarchyHelper.ValidateHierarchy(guild, target, moderator);
        if (hierarchyError is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Cannot Ban", hierarchyError), ephemeral: true);
            return;
        }

        if (deleteDays < 0 || deleteDays > 7)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid Input", "Delete days must be between 0 and 7."), ephemeral: true);
            return;
        }

        try
        {
            await guild.AddBanAsync(target, deleteDays, reason);
            await _modService.BanAsync(guild.Id, guild.Name, target.Id, moderator.Id, reason);

            await FollowupAsync(embed: EmbedHelper.ModerationAction(
                "User Banned",
                target.Username,
                moderator.Mention,
                reason), ephemeral: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error banning user {UserId}", target.Id);
            await FollowupAsync(embed: EmbedHelper.Error("Error", "An unexpected error occurred."), ephemeral: true);
        }
    }

    [SlashCommand("timeout", "Timeout a user")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task TimeoutAsync(
        [Summary("user", "The user to timeout")] SocketGuildUser target,
        [Summary("minutes", "Duration in minutes (max 40320 = 28 days)")] int minutes,
        [Summary("reason", "Reason for the timeout")] string reason = "No reason provided")
    {
        await DeferAsync(ephemeral: true);

        var guild = (SocketGuild)Context.Guild;
        var moderator = (SocketGuildUser)Context.User;

        var hierarchyError = HierarchyHelper.ValidateHierarchy(guild, target, moderator);
        if (hierarchyError is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Cannot Timeout", hierarchyError), ephemeral: true);
            return;
        }

        if (minutes < 1 || minutes > 40320)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid Duration", "Duration must be between 1 and 40320 minutes (28 days)."), ephemeral: true);
            return;
        }

        try
        {
            var duration = TimeSpan.FromMinutes(minutes);
            await target.SetTimeOutAsync(duration);
            await _modService.TimeoutAsync(guild.Id, guild.Name, target.Id, moderator.Id, reason, duration);

            await FollowupAsync(embed: EmbedHelper.ModerationAction(
                "User Timed Out",
                target.Mention,
                moderator.Mention,
                reason,
                extra: $"Duration: {minutes} minute(s)"), ephemeral: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error timing out user {UserId}", target.Id);
            await FollowupAsync(embed: EmbedHelper.Error("Error", "An unexpected error occurred."), ephemeral: true);
        }
    }

    [SlashCommand("untimeout", "Remove timeout from a user")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task UntimeoutAsync(
        [Summary("user", "The user to remove timeout from")] SocketGuildUser target)
    {
        await DeferAsync(ephemeral: true);

        var guild = (SocketGuild)Context.Guild;
        var moderator = (SocketGuildUser)Context.User;

        try
        {
            await target.RemoveTimeOutAsync();
            await _modService.UntimeoutAsync(guild.Id, guild.Name, target.Id, moderator.Id);

            await FollowupAsync(embed: EmbedHelper.Success(
                "Timeout Removed",
                $"Timeout removed from {target.Mention} by {moderator.Mention}."), ephemeral: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing timeout from user {UserId}", target.Id);
            await FollowupAsync(embed: EmbedHelper.Error("Error", "An unexpected error occurred."), ephemeral: true);
        }
    }

    [SlashCommand("warnings", "View warnings for a user")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task WarningsAsync(
        [Summary("user", "The user to check")] SocketGuildUser target)
    {
        await DeferAsync(ephemeral: true);

        var warnings = (await _modService.GetWarningsAsync(Context.Guild.Id, target.Id)).ToList();

        if (warnings.Count == 0)
        {
            await FollowupAsync(embed: EmbedHelper.Info(
                "No Warnings",
                $"{target.Mention} has no active warnings."), ephemeral: true);
            return;
        }

        var description = string.Join("\n", warnings.Select((w, i) =>
            $"`{i + 1}.` {w.Reason} — <t:{new DateTimeOffset(w.CreatedAt).ToUnixTimeSeconds()}:R>"));

        var embed = new EmbedBuilder()
            .WithTitle($"?? Warnings for {target.Username}")
            .WithDescription(description)
            .WithColor(new Color(0xFEE75C))
            .WithFooter($"{warnings.Count} active warning(s)")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }
}
