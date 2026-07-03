namespace Dynamite.Modules.Moderation.Modules;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Moderation.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dynamite.Modules.Moderation.Services;

[RequireContext(ContextType.Guild)]
public class ModerationModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IModerationService _moderationService;
    private readonly ModLogService _modLog;
    private readonly ILogger<ModerationModule> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ModerationModule(
        IModerationService moderationService,
        ModLogService modLog,
        ILogger<ModerationModule> logger,
        IServiceScopeFactory scopeFactory)
    {
        _moderationService = moderationService;
        _modLog = modLog;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    // -------------------------------------------------------------------------
    // /warn
    // -------------------------------------------------------------------------

    [SlashCommand("warn", "Issue a warning to a member")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task WarnAsync(
        [Summary("user", "The member to warn")] SocketGuildUser target,
        [Summary("reason", "Reason for the warning")] string reason)
    {
        await DeferAsync(ephemeral: true);

        var error = HierarchyHelper.ValidateHierarchy(Context.Guild, target, (SocketGuildUser)Context.User);
        if (error is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Cannot warn", error), ephemeral: true);
            return;
        }

        await _moderationService.WarnAsync(
            Context.Guild.Id, Context.Guild.Name,
            target.Id, Context.User.Id, reason, target.Username, Context.User.Username);

        await FollowupAsync(embed: EmbedHelper.Success(
            "Warning issued",
            $"{target.Mention} has been warned.\n**Reason:** {reason}"), ephemeral: true);

        await _modLog.LogAsync(Context.Guild.Id, Context.Guild.Name,
            "Warn", target.Mention, Context.User.Mention, reason);
    }

    // -------------------------------------------------------------------------
    // /kick
    // -------------------------------------------------------------------------

    [SlashCommand("kick", "Kick a member from the server")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    [RequireBotPermission(GuildPermission.KickMembers)]
    public async Task KickAsync(
        [Summary("user", "The member to kick")] SocketGuildUser target,
        [Summary("reason", "Reason for the kick")] string reason = "No reason provided")
    {
        await DeferAsync(ephemeral: true);

        var error = HierarchyHelper.ValidateHierarchy(Context.Guild, target, (SocketGuildUser)Context.User);
        if (error is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Cannot kick", error), ephemeral: true);
            return;
        }

        await _moderationService.KickAsync(
            Context.Guild.Id, Context.Guild.Name,
            target.Id, Context.User.Id, reason, target.Username, Context.User.Username);

        await target.KickAsync(reason);

        await FollowupAsync(embed: EmbedHelper.Success(
            "Member kicked",
            $"{target.Mention} has been kicked.\n**Reason:** {reason}"), ephemeral: true);

        await _modLog.LogAsync(Context.Guild.Id, Context.Guild.Name,
            "Kick", target.Mention, Context.User.Mention, reason);
    }

    // -------------------------------------------------------------------------
    // /ban
    // -------------------------------------------------------------------------

    [SlashCommand("ban", "Ban a member from the server")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task BanAsync(
        [Summary("user", "The member to ban")] SocketGuildUser target,
        [Summary("reason", "Reason for the ban")] string reason,
        [Summary("delete_messages", "Days of messages to delete (0-7)")] int deleteDays = 0)
    {
        await DeferAsync(ephemeral: true);

        var error = HierarchyHelper.ValidateHierarchy(Context.Guild, target, (SocketGuildUser)Context.User);
        if (error is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Cannot ban", error), ephemeral: true);
            return;
        }

        if (deleteDays < 0 || deleteDays > 7)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid value", "Message deletion must be between 0 and 7 days."), ephemeral: true);
            return;
        }

        await _moderationService.BanAsync(
            Context.Guild.Id, Context.Guild.Name,
            target.Id, Context.User.Id, reason, target.Username, Context.User.Username);

        await Context.Guild.AddBanAsync(target, deleteDays, reason);

        await FollowupAsync(embed: EmbedHelper.Success(
            "Member banned",
            $"{target.Mention} has been banned.\n**Reason:** {reason}"), ephemeral: true);

        await _modLog.LogAsync(Context.Guild.Id, Context.Guild.Name,
            "Ban", target.Mention, Context.User.Mention, reason);
    }


    // -------------------------------------------------------------------------
    // /banid — ban by raw user ID (target not in server)
    // -------------------------------------------------------------------------

    [SlashCommand("banid", "Ban a user by Discord ID — they don't have to be in the server")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task BanByIdAsync(
        [Summary("user_id", "The Discord user ID to ban")] string userIdInput,
        [Summary("reason", "Reason for the ban")] string reason,
        [Summary("delete_messages", "Days of messages to delete (0-7)")] int deleteDays = 0,
        [Summary("blacklist", "Add to permanent blacklist — bot will auto-reban if they rejoin")] bool addToBlacklist = false)
    {
        await DeferAsync(ephemeral: true);

        if (!ulong.TryParse(userIdInput, out var userId))
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid ID", "Please provide a valid numeric Discord user ID."), ephemeral: true);
            return;
        }

        if (deleteDays < 0 || deleteDays > 7)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid value", "Message deletion must be between 0 and 7 days."), ephemeral: true);
            return;
        }

        var existingBan = await Context.Guild.GetBanAsync(userId);
        if (existingBan is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Already banned", $"User `{userId}` is already banned."), ephemeral: true);
            return;
        }

        // Snapshot username from Discord before banning.
        // GetUserAsync returns null if the account no longer exists.
        var discordUser = await Context.Client.GetUserAsync(userId);
        var displayName = discordUser?.Username ?? $"Unknown ({userId})";

        await Context.Guild.AddBanAsync(userId, deleteDays, reason);

        await _moderationService.BanByIdAsync(
            Context.Guild.Id, Context.Guild.Name,
            userId, Context.User.Id, reason, displayName, Context.User.Username);

        if (addToBlacklist)
        {
            using var scope = _scopeFactory.CreateScope();
            var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

            await blacklistService.AddAsync(
                Context.Guild.Id, Context.Guild.Name,
                userId,
                discordUser?.Username ?? $"{userId}",
                discordUser?.GetAvatarUrl(),
                Context.User.Id,
                reason);
        }

        var blacklistNote = addToBlacklist
            ? "\n⚠️ Added to permanent blacklist — will be auto-rebanned if they rejoin."
            : string.Empty;

        await FollowupAsync(embed: EmbedHelper.Success(
            "User banned",
            $"**{displayName}** (`{userId}`) has been banned.\n**Reason:** {reason}{blacklistNote}"),
            ephemeral: true);

        await _modLog.LogAsync(Context.Guild.Id, Context.Guild.Name,
            "Ban (by ID)", $"`{displayName}` (`{userId}`)", Context.User.Mention, reason);
    }

    // -------------------------------------------------------------------------
    // /unban � uses ulong ID, not SocketGuildUser (user is not in server)
    // -------------------------------------------------------------------------

    [SlashCommand("unban", "Unban a user by their ID")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task UnbanAsync(
        [Summary("user_id", "The Discord user ID to unban")] string userIdInput,
        [Summary("reason", "Reason for the unban")] string reason)
    {
        await DeferAsync(ephemeral: true);

        // Parse as string input because Discord slash command doesn't have a
        // "banned user" picker � the user is no longer in the server.
        if (!ulong.TryParse(userIdInput, out var userId))
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid ID", "Please provide a valid numeric user ID."), ephemeral: true);
            return;
        }

        // Verify the user is actually banned before attempting removal.
        var ban = await Context.Guild.GetBanAsync(userId);
        if (ban is null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Not banned", $"User `{userId}` is not currently banned."), ephemeral: true);
            return;
        }

        await Context.Guild.RemoveBanAsync(userId);

        await _moderationService.UnbanAsync(
            Context.Guild.Id, Context.Guild.Name,
            userId, Context.User.Id, reason, ban.User.Username, Context.User.Username);

        await FollowupAsync(embed: EmbedHelper.Success(
            "User unbanned",
            $"User `{ban.User.Username}` (`{userId}`) has been unbanned.\n**Reason:** {reason}"), ephemeral: true);

        await _modLog.LogAsync(Context.Guild.Id, Context.Guild.Name,
            "Unban", $"`{ban.User.Username}` (`{userId}`)", Context.User.Mention, reason);
    }

    // -------------------------------------------------------------------------
    // /timeout
    // -------------------------------------------------------------------------

    [SlashCommand("timeout", "Timeout a member")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task TimeoutAsync(
        [Summary("user", "The member to timeout")] SocketGuildUser target,
        [Summary("minutes", "Duration in minutes (5s�28d, enter as fractional minutes)")] double minutes,
        [Summary("reason", "Reason for the timeout")] string reason = "No reason provided")
    {
        await DeferAsync(ephemeral: true);

        var error = HierarchyHelper.ValidateHierarchy(Context.Guild, target, (SocketGuildUser)Context.User);
        if (error is not null)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Cannot timeout", error), ephemeral: true);
            return;
        }

        var duration = TimeSpan.FromMinutes(minutes);

        if (duration.TotalSeconds < 5 || duration.TotalDays > 28)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid duration", "Timeout must be between 5 seconds and 28 days."), ephemeral: true);
            return;
        }

        await _moderationService.TimeoutAsync(
            Context.Guild.Id, Context.Guild.Name,
            target.Id, Context.User.Id, reason, duration, target.Username, Context.User.Username);

        await target.SetTimeOutAsync(duration);

        var humanDuration = FormatDuration(duration);
        await FollowupAsync(embed: EmbedHelper.Success(
            "Member timed out",
            $"{target.Mention} has been timed out for **{humanDuration}**.\n**Reason:** {reason}"), ephemeral: true);

        await _modLog.LogAsync(Context.Guild.Id, Context.Guild.Name,
            "Timeout", target.Mention, Context.User.Mention, reason, $"Duration: {humanDuration}");
    }

    // -------------------------------------------------------------------------
    // /untimeout
    // -------------------------------------------------------------------------

    [SlashCommand("untimeout", "Remove a timeout from a member")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task UntimeoutAsync(
        [Summary("user", "The member to remove timeout from")] SocketGuildUser target)
    {
        await DeferAsync(ephemeral: true);

        if (target.TimedOutUntil is null || target.TimedOutUntil <= DateTimeOffset.UtcNow)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Not timed out", $"{target.Mention} is not currently timed out."), ephemeral: true);
            return;
        }

        await target.RemoveTimeOutAsync();

        await _moderationService.UntimeoutAsync(
            Context.Guild.Id, Context.Guild.Name,
            target.Id, Context.User.Id, target.Username, Context.User.Username);

        await FollowupAsync(embed: EmbedHelper.Success(
            "Timeout removed",
            $"Timeout has been removed from {target.Mention}."), ephemeral: true);

        await _modLog.LogAsync(Context.Guild.Id, Context.Guild.Name,
            "Untimeout", target.Mention, Context.User.Mention, "Timeout removed");
    }

    // -------------------------------------------------------------------------
    // /warnings
    // -------------------------------------------------------------------------

    [SlashCommand("warnings", "View active warnings for a member")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task WarningsAsync(
        [Summary("user", "The member to check warnings for")] SocketGuildUser target)
    {
        await DeferAsync(ephemeral: true);

        var warnings = (await _moderationService.GetWarningsAsync(Context.Guild.Id, target.Id)).ToList();

        if (warnings.Count == 0)
        {
            await FollowupAsync(embed: EmbedHelper.Info(
                "No warnings",
                $"{target.Mention} has no active warnings."), ephemeral: true);
            return;
        }

        var description = string.Join("\n\n", warnings.Select((w, i) =>
            $"**#{i + 1}** � {w.CreatedAt:yyyy-MM-dd HH:mm} UTC\n" +
            $"Reason: {w.Reason}"));

        await FollowupAsync(embed: EmbedHelper.Warn(
            $"Warnings for {target.Username} ({warnings.Count})",
            description), ephemeral: true);
    }

    // -------------------------------------------------------------------------
    // /purge � bulk delete messages in current channel
    // -------------------------------------------------------------------------

    [SlashCommand("purge", "Delete multiple messages from this channel")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    public async Task PurgeAsync(
        [Summary("amount", "Number of messages to delete (1�100)")] int amount,
        [Summary("user", "Only delete messages from this user (optional)")] SocketGuildUser? filterUser = null)
    {
        await DeferAsync(ephemeral: true);

        if (amount < 1 || amount > 100)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid amount", "Please specify between 1 and 100 messages."), ephemeral: true);
            return;
        }

        if (Context.Channel is not ITextChannel textChannel)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Error", "This command can only be used in text channels."), ephemeral: true);
            return;
        }

        // Fetch more messages than requested when filtering by user,
        // since we need to scan past non-matching messages.
        var fetchCount = filterUser is not null ? Math.Min(amount * 3, 300) : amount;
        var messages = await textChannel.GetMessagesAsync(fetchCount).FlattenAsync();

        // Discord only allows bulk deletion of messages under 14 days old.
        // Silently skip older messages rather than throwing � the moderator
        // gets a count of what was actually deleted.
        var cutoff = DateTimeOffset.UtcNow.AddDays(-14);
        var eligible = messages
            .Where(m => m.Timestamp > cutoff)
            .Where(m => filterUser is null || m.Author.Id == filterUser.Id)
            .Take(amount)
            .ToList();

        if (eligible.Count == 0)
        {
            await FollowupAsync(embed: EmbedHelper.Error(
                "Nothing to delete",
                "No eligible messages found. Messages older than 14 days cannot be bulk deleted."), ephemeral: true);
            return;
        }

        await textChannel.DeleteMessagesAsync(eligible);

        var filterNote = filterUser is not null ? $" from {filterUser.Mention}" : string.Empty;
        await FollowupAsync(embed: EmbedHelper.Success(
            "Messages deleted",
            $"Deleted **{eligible.Count}** message(s){filterNote}."), ephemeral: true);

        _logger.LogInformation(
            "Purge: {Count} messages deleted in #{Channel} by {Mod} in guild {Guild}",
            eligible.Count, textChannel.Name, Context.User.Id, Context.Guild.Id);
    }

    // -------------------------------------------------------------------------
    // /slowmode � set or clear channel slowmode
    // -------------------------------------------------------------------------

    [SlashCommand("slowmode", "Set or disable slowmode in this channel")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    [RequireBotPermission(GuildPermission.ManageChannels)]
    public async Task SlowmodeAsync(
        [Summary("seconds", "Slowmode interval in seconds (0 = disable, max 21600)")] int seconds)
    {
        await DeferAsync(ephemeral: true);

        // Discord limit: 0�21600 seconds (6 hours)
        if (seconds < 0 || seconds > 21600)
        {
            await FollowupAsync(embed: EmbedHelper.Error(
                "Invalid value",
                "Slowmode must be between 0 (disabled) and 21600 seconds (6 hours)."), ephemeral: true);
            return;
        }

        if (Context.Channel is not ITextChannel textChannel)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Error", "This command can only be used in text channels."), ephemeral: true);
            return;
        }

        await textChannel.ModifyAsync(p => p.SlowModeInterval = seconds);

        var message = seconds == 0
            ? $"Slowmode has been **disabled** in {textChannel.Mention}."
            : $"Slowmode set to **{FormatDuration(TimeSpan.FromSeconds(seconds))}** in {textChannel.Mention}.";

        await FollowupAsync(embed: EmbedHelper.Success("Slowmode updated", message), ephemeral: true);

        _logger.LogInformation(
            "Slowmode set to {Seconds}s in #{Channel} by {Mod} in guild {Guild}",
            seconds, textChannel.Name, Context.User.Id, Context.Guild.Id);
    }


    // -------------------------------------------------------------------------
    // /modhistory — ban audit history for a user (works even if they left)
    // -------------------------------------------------------------------------

    [SlashCommand("modhistory", "View complete ban/blacklist history for a user by their ID")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task ModHistoryAsync(
        [Summary("user_id", "Discord user ID to look up")] string userIdInput)
    {
        await DeferAsync(ephemeral: true);

        if (!ulong.TryParse(userIdInput, out var userId))
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid ID", "Please provide a valid numeric Discord user ID."), ephemeral: true);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();

        var history = (await moderationService.GetBanHistoryAsync(Context.Guild.Id, userId)).ToList();

        if (history.Count == 0)
        {
            await FollowupAsync(embed: EmbedHelper.Info(
                "No ban history",
                $"No ban-related records found for user `{userId}` in this server."), ephemeral: true);
            return;
        }

        static string Label(Dynamite.Core.Enums.ModerationActionType t) => t switch
        {
            Dynamite.Core.Enums.ModerationActionType.Ban         => "🔨 Ban",
            Dynamite.Core.Enums.ModerationActionType.BanId       => "🔨 Ban (by ID)",
            Dynamite.Core.Enums.ModerationActionType.Unban       => "✅ Unban",
            Dynamite.Core.Enums.ModerationActionType.Blacklist   => "⛔ Blacklist",
            Dynamite.Core.Enums.ModerationActionType.Unblacklist => "🟢 Unblacklist",
            _                                                      => t.ToString()
        };

        var lines = history.Select(a =>
            $"{Label(a.ActionType)} — <t:{new DateTimeOffset(a.CreatedAt).ToUnixTimeSeconds()}:f>\n" +
            $"› **Reason:** {a.Reason}\n" +
            $"› **Mod:** <@{a.ModeratorId}>");

        // Discord embed description cap is 4096 chars; truncate if needed.
        var description = string.Join("\n\n", lines);
        if (description.Length > 3900)
            description = description[..3900] + "\n\n*... (truncated, use dashboard for full history)*";

        var embed = new EmbedBuilder()
            .WithTitle($"📋 Ban History — {history.Count} record(s)")
            .WithDescription(description)
            .WithColor(new Color(0xFF4444))
            .WithFooter($"User ID: {userId} • Records are permanent and cannot be modified")
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{(int)duration.TotalMinutes}m";
    }
}
