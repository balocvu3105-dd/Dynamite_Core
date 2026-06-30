// src/Dynamite.Modules/Moderation/Modules/BlacklistModule.cs
namespace Dynamite.Modules.Moderation.Modules;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Moderation.Helpers;
using Microsoft.Extensions.Logging;

/// <summary>
/// /blacklist commands — manage the persistent guild blacklist.
///
/// The blacklist is our own database table, separate from Discord's ban list.
/// Its key advantage: data persists even after a user leaves or deletes their account,
/// and the BlacklistEventHandler auto-rebans anyone on the list who tries to rejoin.
/// </summary>
[Group("blacklist", "Manage the permanent server blacklist")]
[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.BanMembers)]
public class BlacklistModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IBlacklistService _blacklistService;
    private readonly ILogger<BlacklistModule> _logger;

    public BlacklistModule(
        IBlacklistService blacklistService,
        ILogger<BlacklistModule> logger)
    {
        _blacklistService = blacklistService;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // /blacklist add
    // -------------------------------------------------------------------------

    [SlashCommand("add", "Add a user to the permanent blacklist (and optionally ban them now)")]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task AddAsync(
        [Summary("user_id", "Discord user ID to blacklist")] string userIdInput,
        [Summary("reason", "Reason for the blacklist")] string reason,
        [Summary("notes", "Extra notes (alt accounts, context, etc.)")] string? notes = null,
        [Summary("ban_now", "Also issue a Discord ban immediately (default: true)")] bool banNow = true)
    {
        await DeferAsync(ephemeral: true);

        if (!ulong.TryParse(userIdInput, out var userId))
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid ID", "Please provide a valid numeric Discord user ID."), ephemeral: true);
            return;
        }

        // Check if already blacklisted.
        var alreadyBlacklisted = await _blacklistService.IsBlacklistedAsync(Context.Guild.Id, userId);
        if (alreadyBlacklisted)
        {
            await FollowupAsync(embed: EmbedHelper.Warn("Already blacklisted", $"User `{userId}` is already on the blacklist."), ephemeral: true);
            return;
        }

        // Resolve username snapshot.
        var discordUser = await Context.Client.GetUserAsync(userId);
        var displayName = discordUser?.Username ?? $"{userId}";

        await _blacklistService.AddAsync(
            Context.Guild.Id, Context.Guild.Name,
            userId,
            displayName,
            discordUser?.GetAvatarUrl(),
            Context.User.Id,
            reason,
            notes);

        string? banNote = null;
        if (banNow)
        {
            var existingBan = await Context.Guild.GetBanAsync(userId);
            if (existingBan is null)
            {
                await Context.Guild.AddBanAsync(userId, 0, reason);
                banNote = "Discord ban issued.";
            }
            else
            {
                banNote = "User was already Discord-banned.";
            }
        }

        var description = $"**User:** `{displayName}` (`{userId}`)\n**Reason:** {reason}";
        if (notes is not null) description += $"\n**Notes:** {notes}";
        if (banNote is not null) description += $"\n{banNote}";

        await FollowupAsync(embed: EmbedHelper.Success("User blacklisted", description), ephemeral: true);

        _logger.LogInformation("User {UserId} added to blacklist in guild {GuildId} by {ModId}",
            userId, Context.Guild.Id, Context.User.Id);
    }

    // -------------------------------------------------------------------------
    // /blacklist remove
    // -------------------------------------------------------------------------

    [SlashCommand("remove", "Remove a user from the blacklist")]
    public async Task RemoveAsync(
        [Summary("user_id", "Discord user ID to remove")] string userIdInput,
        [Summary("reason", "Reason for removing")] string reason = "No reason provided",
        [Summary("unban", "Also unban them from Discord (default: false)")] bool unban = false)
    {
        await DeferAsync(ephemeral: true);

        if (!ulong.TryParse(userIdInput, out var userId))
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid ID", "Please provide a valid numeric Discord user ID."), ephemeral: true);
            return;
        }

        try
        {
            await _blacklistService.RemoveAsync(Context.Guild.Id, userId, Context.User.Id, reason);
        }
        catch (KeyNotFoundException)
        {
            await FollowupAsync(embed: EmbedHelper.Error("Not found", $"User `{userId}` is not on the blacklist."), ephemeral: true);
            return;
        }

        string? unbanNote = null;
        if (unban)
        {
            var ban = await Context.Guild.GetBanAsync(userId);
            if (ban is not null)
            {
                await Context.Guild.RemoveBanAsync(userId);
                unbanNote = $"Discord ban lifted for `{ban.User.Username}`.";
            }
            else
            {
                unbanNote = "User was not Discord-banned.";
            }
        }

        var description = $"User `{userId}` removed from blacklist.\n**Reason:** {reason}";
        if (unbanNote is not null) description += $"\n{unbanNote}";

        await FollowupAsync(embed: EmbedHelper.Success("Blacklist entry removed", description), ephemeral: true);
    }

    // -------------------------------------------------------------------------
    // /blacklist check
    // -------------------------------------------------------------------------

    [SlashCommand("check", "Check if a user is on the blacklist")]
    public async Task CheckAsync(
        [Summary("user_id", "Discord user ID to check")] string userIdInput)
    {
        await DeferAsync(ephemeral: true);

        if (!ulong.TryParse(userIdInput, out var userId))
        {
            await FollowupAsync(embed: EmbedHelper.Error("Invalid ID", "Please provide a valid numeric Discord user ID."), ephemeral: true);
            return;
        }

        var entry = await _blacklistService.GetAsync(Context.Guild.Id, userId);

        if (entry is null)
        {
            await FollowupAsync(embed: EmbedHelper.Info(
                "Not blacklisted",
                $"User `{userId}` is not on the blacklist."), ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("⛔ Blacklisted User")
            .WithColor(new Color(0xFF0000))
            .AddField("User", $"`{entry.TargetUsername}` (`{entry.TargetUserId}`)", inline: true)
            .AddField("Blacklisted", $"<t:{new DateTimeOffset(entry.CreatedAt).ToUnixTimeSeconds()}:R>", inline: true)
            .AddField("Reason", entry.Reason)
            .WithFooter($"Mod ID: {entry.ModeratorId}");

        if (entry.Notes is not null)
            embed.AddField("Notes", entry.Notes);

        if (entry.TargetAvatarUrl is not null)
            embed.WithThumbnailUrl(entry.TargetAvatarUrl);

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    // -------------------------------------------------------------------------
    // /blacklist list
    // -------------------------------------------------------------------------

    [SlashCommand("list", "Show the most recent blacklisted users (max 25)")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);

        var entries = (await _blacklistService.GetAllAsync(Context.Guild.Id, count: 25)).ToList();

        if (entries.Count == 0)
        {
            await FollowupAsync(embed: EmbedHelper.Info("Blacklist empty", "No users are currently blacklisted."), ephemeral: true);
            return;
        }

        var lines = entries.Select((e, i) =>
            $"**#{i + 1}** `{e.TargetUsername}` (`{e.TargetUserId}`) — {e.Reason} " +
            $"<t:{new DateTimeOffset(e.CreatedAt).ToUnixTimeSeconds()}:d>");

        var embed = new EmbedBuilder()
            .WithTitle($"⛔ Blacklist — {entries.Count} entries")
            .WithColor(new Color(0xFF4444))
            .WithDescription(string.Join("\n", lines))
            .WithFooter("Use /blacklist check <id> for full details")
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }
}
