// src/Dynamite.Modules/Logging/Loggers/VoiceLogger.cs
namespace Dynamite.Modules.Logging.Loggers;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Dynamite.Modules.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class VoiceLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<VoiceLogger> _logger;

    public VoiceLogger(
        IServiceScopeFactory scopeFactory,
        DiscordSocketClient client,
        ILogger<VoiceLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _logger = logger;
    }

    public async Task OnUserVoiceStateUpdatedAsync(
        SocketUser user,
        SocketVoiceState before,
        SocketVoiceState after)
    {
        if (user is not SocketGuildUser guildUser) return;

        var guildId = guildUser.Guild.Id;
        Embed? embed = null;

        var joined = before.VoiceChannel is null && after.VoiceChannel is not null;
        var left = before.VoiceChannel is not null && after.VoiceChannel is null;
        var moved = before.VoiceChannel is not null && after.VoiceChannel is not null
                     && before.VoiceChannel.Id != after.VoiceChannel.Id;

        if (joined)
            embed = LogEmbedHelper.VoiceJoined(user.ToString(), user.Id, after.VoiceChannel!.Name);
        else if (left)
            embed = LogEmbedHelper.VoiceLeft(user.ToString(), user.Id, before.VoiceChannel!.Name);
        else if (moved)
            embed = LogEmbedHelper.VoiceMoved(user.ToString(), user.Id,
                before.VoiceChannel!.Name, after.VoiceChannel!.Name);

        if (embed is null) return;

        using var scope = _scopeFactory.CreateScope();
        var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();

        var title = embed.Title ?? "Voice Activity";
        var description = embed.Description ?? string.Join("\n", embed.Fields.Select(f => $"{f.Name}: {f.Value}"));
        await logService.LogActivityAsync(guildId, LogCategory.Voice, "VoiceStateUpdated", title, description, user.Id.ToString(), user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

        var channelId = await logService.GetLogChannelAsync(guildId, LogCategory.Voice);
        if (channelId is null) return;

        await SendToChannelAsync(guildId, channelId.Value, embed);
    }

    private async Task SendToChannelAsync(ulong guildId, ulong channelId, Embed embed)
    {
        try
        {
            var guild = _client.GetGuild(guildId);
            var channel = guild?.GetTextChannel(channelId);
            if (channel is null)
            {
                _logger.LogWarning("Voice log channel {ChannelId} not found in guild {GuildId}", channelId, guildId);
                return;
            }
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send voice log to channel {ChannelId}", channelId);
        }
    }
}