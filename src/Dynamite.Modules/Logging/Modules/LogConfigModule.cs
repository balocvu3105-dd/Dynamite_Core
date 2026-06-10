// src/Dynamite.Modules/Logging/Modules/LogConfigModule.cs
namespace Dynamite.Modules.Logging.Modules;

using Discord;
using Discord.Interactions;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Microsoft.Extensions.Logging;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
[Group("logset", "Configure server logging channels")]
public class LogConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IServerLogService _logService;
    private readonly ILogger<LogConfigModule> _logger;

    public LogConfigModule(IServerLogService logService, ILogger<LogConfigModule> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    [SlashCommand("messages", "Set the channel for message logs (delete/edit)")]
    public async Task SetMessagesAsync(
        [Summary("channel", "Channel to send message logs")] ITextChannel channel)
        => await SetChannelAsync(LogCategory.Message, channel);

    [SlashCommand("members", "Set the channel for member logs (join/leave/roles)")]
    public async Task SetMembersAsync(
        [Summary("channel", "Channel to send member logs")] ITextChannel channel)
        => await SetChannelAsync(LogCategory.Member, channel);

    [SlashCommand("voice", "Set the channel for voice logs (join/leave/move)")]
    public async Task SetVoiceAsync(
        [Summary("channel", "Channel to send voice logs")] ITextChannel channel)
        => await SetChannelAsync(LogCategory.Voice, channel);

    [SlashCommand("server", "Set the channel for server logs (channels/roles created/deleted)")]
    public async Task SetServerAsync(
        [Summary("channel", "Channel to send server logs")] ITextChannel channel)
        => await SetChannelAsync(LogCategory.Server, channel);

    // Audit log — hạn chế hơn: chỉ Server Owner hoặc Administrator
    // Tự động lock channel: chỉ bot được Send Messages
    [SlashCommand("audit", "Set the immutable audit log channel (owner/admin only)")]
    public async Task SetAuditAsync(
        [Summary("channel", "Channel for the immutable audit log")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        // Extra guard: chỉ Server Owner hoặc Administrator
        var guildUser = Context.User as IGuildUser;
        if (guildUser is null || (!guildUser.GuildPermissions.Administrator && Context.Guild.OwnerId != guildUser.Id))
        {
            await FollowupAsync(embed: ErrorEmbed("Permission Denied",
                "Only the Server Owner or Administrators can configure the audit log."), ephemeral: true);
            return;
        }

        try
        {
            // Save channel to DB
            await _logService.SetLogChannelAsync(
                Context.Guild.Id,
                Context.Guild.Name,
                LogCategory.Audit,
                channel.Id);

            // Lock channel: deny Send Messages for @everyone and all roles
            // Bot keeps its own permissions via role hierarchy
            await LockAuditChannelAsync(channel);

            var embed = new EmbedBuilder()
                .WithTitle("🔒 Audit Log Channel Set")
                .WithDescription($"Audit logs will be sent to {channel.Mention}.\n\nThe channel has been **locked** — only the bot can post here.")
                .WithColor(new Color(0x2C3E50))
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await FollowupAsync(embed: embed, ephemeral: true);

            _logger.LogInformation("Audit log channel set to {ChannelId} in guild {GuildId}",
                channel.Id, Context.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting audit log channel for guild {GuildId}", Context.Guild.Id);
            await FollowupAsync(embed: ErrorEmbed("Error", "An unexpected error occurred. Check bot permissions."), ephemeral: true);
        }
    }

    [SlashCommand("view", "View current logging configuration")]
    public async Task ViewAsync()
    {
        await DeferAsync(ephemeral: true);

        var guildId = Context.Guild.Id;

        var msgCh    = await _logService.GetLogChannelAsync(guildId, LogCategory.Message);
        var memCh    = await _logService.GetLogChannelAsync(guildId, LogCategory.Member);
        var voiceCh  = await _logService.GetLogChannelAsync(guildId, LogCategory.Voice);
        var serverCh = await _logService.GetLogChannelAsync(guildId, LogCategory.Server);
        var auditCh  = await _logService.GetLogChannelAsync(guildId, LogCategory.Audit);

        static string Fmt(ulong? id) => id.HasValue ? $"<#{id}>" : "*not set*";

        var embed = new EmbedBuilder()
            .WithTitle("📋 Logging Configuration")
            .WithColor(new Color(0x5865F2))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("📩 Messages", Fmt(msgCh), inline: true)
            .AddField("👥 Members", Fmt(memCh), inline: true)
            .AddField("🔊 Voice", Fmt(voiceCh), inline: true)
            .AddField("🖥️ Server", Fmt(serverCh), inline: true)
            .AddField("🔒 Audit (immutable)", Fmt(auditCh), inline: true)
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    private async Task SetChannelAsync(LogCategory category, ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            await _logService.SetLogChannelAsync(
                Context.Guild.Id,
                Context.Guild.Name,
                category,
                channel.Id);

            var embed = new EmbedBuilder()
                .WithTitle("✅ Log Channel Set")
                .WithDescription($"{category} logs will be sent to {channel.Mention}.")
                .WithColor(new Color(0x57F287))
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await FollowupAsync(embed: embed, ephemeral: true);

            _logger.LogInformation("Log channel for {Category} set to {ChannelId} in guild {GuildId}",
                category, channel.Id, Context.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting log channel for guild {GuildId}", Context.Guild.Id);
            await FollowupAsync("An unexpected error occurred.", ephemeral: true);
        }
    }

    // Lock audit channel: deny Send Messages for @everyone
    // Bot sẽ vẫn post được vì bot role có permissions riêng
    private async Task LockAuditChannelAsync(ITextChannel channel)
    {
        try
        {
            // Deny @everyone Send Messages + Add Reactions
            await channel.AddPermissionOverwriteAsync(
                Context.Guild.EveryoneRole,
                new OverwritePermissions(
                    sendMessages: PermValue.Deny,
                    addReactions: PermValue.Deny,
                    createPublicThreads: PermValue.Deny,
                    createPrivateThreads: PermValue.Deny));

            // Deny all non-bot roles Send Messages
            foreach (var role in Context.Guild.Roles)
            {
                // Skip @everyone (đã xử lý) và managed roles (bot roles)
                if (role.IsEveryone) continue;
                if (role.IsManaged) continue; // managed = bot/integration roles

                await channel.AddPermissionOverwriteAsync(
                    role,
                    new OverwritePermissions(
                        sendMessages: PermValue.Deny,
                        addReactions: PermValue.Deny));
            }

            _logger.LogInformation("Audit channel {ChannelId} locked in guild {GuildId}",
                channel.Id, Context.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fully lock audit channel {ChannelId} — bot may lack Manage Channel permission",
                channel.Id);
        }
    }

    private static Embed ErrorEmbed(string title, string description)
        => new EmbedBuilder()
            .WithTitle($"❌ {title}")
            .WithDescription(description)
            .WithColor(new Color(0xED4245))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
}