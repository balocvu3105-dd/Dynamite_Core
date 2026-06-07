// src/Dynamite.Modules/Logging/Loggers/MessageLogger.cs
namespace Dynamite.Modules.Logging.Loggers;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Dynamite.Modules.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class MessageLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<MessageLogger> _logger;

    public MessageLogger(
        IServiceScopeFactory scopeFactory,
        DiscordSocketClient client,
        ILogger<MessageLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _logger = logger;
    }

    public async Task OnMessageDeletedAsync(
        Cacheable<IMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel)
    {
        if (!message.HasValue) return;
        var msg = message.Value;
        if (msg.Author.IsBot) return;

        // Fix: resolve channel riêng, không dùng ternary với 2 incompatible types
        ITextChannel? textChannel = null;
        if (channel.HasValue && channel.Value is ITextChannel tc)
            textChannel = tc;
        else if (_client.GetChannel(channel.Id) is ITextChannel stc)
            textChannel = stc;

        if (textChannel is null) return;

        var embed = LogEmbedHelper.MessageDeleted(
            msg.Author.ToString() ?? msg.Author.Username,
            msg.Author.Id,
            $"<#{textChannel.Id}>",
            msg.Content);

        await SendLogAsync(textChannel.GuildId, LogCategory.Message, embed);
    }

    public async Task OnMessagesBulkDeletedAsync(
        IReadOnlyCollection<Cacheable<IMessage, ulong>> messages,
        Cacheable<IMessageChannel, ulong> channel)
    {
        // Fix: same pattern — resolve channel riêng
        ITextChannel? textChannel = null;
        if (channel.HasValue && channel.Value is ITextChannel tc)
            textChannel = tc;
        else if (_client.GetChannel(channel.Id) is ITextChannel stc)
            textChannel = stc;

        if (textChannel is null) return;

        var embed = LogEmbedHelper.MessagesBulkDeleted($"<#{textChannel.Id}>", messages.Count);
        await SendLogAsync(textChannel.GuildId, LogCategory.Message, embed);
    }

    public async Task OnMessageUpdatedAsync(
        Cacheable<IMessage, ulong> before,
        SocketMessage after,
        ISocketMessageChannel channel)
    {
        if (!before.HasValue) return;
        var beforeMsg = before.Value;
        if (after.Author.IsBot) return;
        if (beforeMsg.Content == after.Content) return;
        if (channel is not ITextChannel textChannel) return;

        var embed = LogEmbedHelper.MessageEdited(
            after.Author.ToString() ?? after.Author.Username,
            after.Author.Id,
            $"<#{channel.Id}>",
            beforeMsg.Content,
            after.Content,
            after.GetJumpUrl());

        await SendLogAsync(textChannel.GuildId, LogCategory.Message, embed);
    }

    private async Task SendLogAsync(ulong guildId, LogCategory category, Embed embed)
    {
        using var scope = _scopeFactory.CreateScope();
        var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();

        var channelId = await logService.GetLogChannelAsync(guildId, category);
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
                _logger.LogWarning("Log channel {ChannelId} not found in guild {GuildId}", channelId, guildId);
                return;
            }
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send log to channel {ChannelId}", channelId);
        }
    }
}