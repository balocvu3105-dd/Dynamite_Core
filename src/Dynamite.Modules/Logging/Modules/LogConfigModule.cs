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

    [SlashCommand("view", "View current logging configuration")]
    public async Task ViewAsync()
    {
        await DeferAsync(ephemeral: true);

        var guildId = Context.Guild.Id;

        var msgCh = await _logService.GetLogChannelAsync(guildId, LogCategory.Message);
        var memCh = await _logService.GetLogChannelAsync(guildId, LogCategory.Member);
        var voiceCh = await _logService.GetLogChannelAsync(guildId, LogCategory.Voice);
        var serverCh = await _logService.GetLogChannelAsync(guildId, LogCategory.Server);

        static string Fmt(ulong? id) => id.HasValue ? $"<#{id}>" : "*not set*";

        var embed = new EmbedBuilder()
            .WithTitle("📋 Logging Configuration")
            .WithColor(new Color(0x5865F2))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("📩 Messages", Fmt(msgCh), inline: true)
            .AddField("👥 Members", Fmt(memCh), inline: true)
            .AddField("🔊 Voice", Fmt(voiceCh), inline: true)
            .AddField("🖥️ Server", Fmt(serverCh), inline: true)
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
}