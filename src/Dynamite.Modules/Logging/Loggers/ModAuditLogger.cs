// src/Dynamite.Modules/Logging/Loggers/ModAuditLogger.cs
namespace Dynamite.Modules.Logging.Loggers;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Dynamite.Modules.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logs every mod-level slash command execution to the guild's audit channel.
/// Captures: command name, moderator, channel, arguments, success/failure.
/// Called from BotHostedService.OnInteractionExecutedAsync for BOTH success and failure.
///
/// Design: Singleton — injected into BotHostedService.
/// Uses IServiceScopeFactory to access scoped services (IServerLogService).
///
/// Which commands are audited:
/// We whitelist by top-level command name. Any slash command whose root name
/// appears in ModCommandRoots will be sent to the audit channel.
/// This covers ban, kick, warn, purge, slowmode, blacklist, modlog, setup, etc.
/// </summary>
public class ModAuditLogger
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ModAuditLogger> _logger;

    /// <summary>
    /// Top-level command names that require mod-level permissions.
    /// Subcommand groups (e.g. /blacklist add) are matched by their root "blacklist".
    /// </summary>
    private static readonly HashSet<string> ModCommandRoots = new(StringComparer.OrdinalIgnoreCase)
    {
        // Phase 2 — Moderation
        "ban", "banid", "unban", "kick", "warn", "warnings",
        "timeout", "untimeout", "purge", "slowmode", "modhistory",

        // Phase 2+ — Blacklist
        "blacklist",

        // Phase 2+ — Mod log queries
        "modlog",

        // Phase 3 — Role management
        "autorole", "rolepanel",

        // Phase 4 — Setup
        "setup",

        // Phase 6 — Logging config
        "logconfig",

        // Phase 7 — Welcome config
        "welcomeconfig", "verifyconfig",

        // Phase 8 — Anti-spam config
        "antispam",

        // Phase 10a — Giveaway (mod actions)
        "giveaway",

        // Phase 10b — Ticket config
        "ticketconfig",

        // Phase 5 — Temp voice config
        "voiceconfig",
    };

    public ModAuditLogger(
        DiscordSocketClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<ModAuditLogger> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Entry point called from BotHostedService.OnInteractionExecutedAsync.
    /// Returns immediately if the command is not mod-relevant or has no guild context.
    /// </summary>
    public async Task LogAsync(
        ICommandInfo? command,
        IInteractionContext ctx,
        IResult result)
    {
        // DMs and non-guild contexts have no audit channel to write to.
        if (ctx.Guild is null || command is null) return;

        var fullName = GetFullCommandName(command);

        // Only audit mod-level commands.
        if (!IsModCommand(fullName)) return;

        var formattedArgs = ExtractArgs(ctx.Interaction);

        var embed = LogEmbedHelper.ModCommandExecuted(
            fullName,
            ctx.User.ToString() ?? ctx.User.Username,
            ctx.User.Id,
            ctx.Channel?.Name ?? "unknown",
            formattedArgs,
            result.IsSuccess,
            result.IsSuccess ? null : (result.ErrorReason ?? "Unknown error"));

        await SendToAuditChannelAsync(ctx.Guild.Id, embed);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns "group subcommand" for grouped commands, or just "name" for top-level.
    /// E.g.: blacklist add → "blacklist add", ban → "ban"
    /// </summary>
    private static string GetFullCommandName(ICommandInfo command)
    {
        var group = command.Module?.SlashGroupName;
        return string.IsNullOrEmpty(group) ? command.Name : $"{group} {command.Name}";
    }

    private static bool IsModCommand(string fullName)
    {
        var root = fullName.Split(' ')[0];
        return ModCommandRoots.Contains(root);
    }

    /// <summary>
    /// Walks the interaction's option tree and formats them as "key: `value`" pairs.
    /// Handles subcommand groups, subcommands, and regular options.
    /// </summary>
    private static string ExtractArgs(IDiscordInteraction interaction)
    {
        if (interaction is not SocketSlashCommand slash)
            return string.Empty;

        return FormatOptions(slash.Data.Options);
    }

    private static string FormatOptions(
        IReadOnlyCollection<SocketSlashCommandDataOption>? options)
    {
        if (options is null || options.Count == 0) return string.Empty;

        var parts = new List<string>();

        foreach (var opt in options)
        {
            if (opt.Type is ApplicationCommandOptionType.SubCommand
                         or ApplicationCommandOptionType.SubCommandGroup)
            {
                // Recurse into subcommand options; prepend the subcommand name
                var inner = FormatOptions(opt.Options as IReadOnlyCollection<SocketSlashCommandDataOption>);
                parts.Add(string.IsNullOrEmpty(inner) ? $"[{opt.Name}]" : $"[{opt.Name}] {inner}");
            }
            else
            {
                var rawValue = opt.Value switch
                {
                    IUser u    => $"@{u.Username} ({u.Id})",
                    IRole r    => $"@{r.Name} ({r.Id})",
                    bool b     => b ? "true" : "false",
                    _          => opt.Value?.ToString() ?? "null"
                };

                // Truncate individual values so one long reason doesn't break the embed.
                if (rawValue.Length > 80) rawValue = rawValue[..77] + "...";

                parts.Add($"{opt.Name}: `{rawValue}`");
            }
        }

        return string.Join("  ·  ", parts);
    }

    private async Task SendToAuditChannelAsync(ulong guildId, Embed embed)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();

            var title = embed.Title ?? embed.Author?.Name ?? "Moderation Audit";
            var description = embed.Description ?? string.Join("\n", embed.Fields.Select(f => $"{f.Name}: {f.Value}"));
            await logService.LogActivityAsync(guildId, LogCategory.Moderation, "ModAction", title, description);

            var channelId = await logService.GetLogChannelAsync(guildId, LogCategory.Audit);
            if (channelId is null) return;

            var guild = _client.GetGuild(guildId);
            var channel = guild?.GetTextChannel(channelId.Value);
            if (channel is null)
            {
                _logger.LogWarning(
                    "Audit channel {ChannelId} not found in guild {GuildId}",
                    channelId.Value, guildId);
                return;
            }

            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ModAuditLogger failed to send to audit channel in guild {GuildId}", guildId);
        }
    }
}
