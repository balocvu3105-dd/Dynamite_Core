namespace Dynamite.Modules.Moderation.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Moderation.Helpers;
using Microsoft.Extensions.Logging;

public class ModLogService
{
    private readonly IGuildConfigService _configService;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<ModLogService> _logger;

    public ModLogService(
        IGuildConfigService configService,
        DiscordSocketClient client,
        ILogger<ModLogService> logger)
    {
        _configService = configService;
        _client = client;
        _logger = logger;
    }

    public async Task LogAsync(
        ulong guildId, string guildName,
        string action, string targetMention, string moderatorMention,
        string reason, string? extra = null)
    {
        _logger.LogInformation("ModLog called — action: {Action}, guild: {GuildId}", action, guildId);
        try
        {
            var config = await _configService.GetOrCreateConfigAsync(guildId, guildName);
            _logger.LogInformation("ModLog config — ChannelId: {ChannelId}", config.ModLogChannelId);

            if (config.ModLogChannelId is null)
            {
                _logger.LogInformation("ModLog skipped — no channel configured for guild {GuildId}", guildId);
                return;
            }

            var channelId = config.ModLogChannelId.Value;
            var channel = await ResolveTextChannelAsync(guildId, channelId);

            if (channel is null)
            {
                _logger.LogWarning(
                    "Mod log channel {ChannelId} not found or is not a text channel in guild {GuildId}",
                    channelId, guildId);
                return;
            }

            var embed = EmbedHelper.ModerationAction(action, targetMention, moderatorMention, reason, extra);
            await channel.SendMessageAsync(embed: embed);
            _logger.LogInformation("ModLog sent successfully to #{Channel}", channel.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send mod log for guild {GuildId}", guildId);
        }
    }

    // Fix: separate method to safely resolve a text channel from cache or REST.
    // The old code did a direct cast from RestChannel → ITextChannel which throws
    // an InvalidCastException if the configured channel is not a text channel.
    // We check the type explicitly after the REST fallback instead.
    private async Task<ITextChannel?> ResolveTextChannelAsync(ulong guildId, ulong channelId)
    {
        var guild = _client.GetGuild(guildId);

        // Try cache first (no network call, always prefer this)
        var cached = guild?.GetTextChannel(channelId);
        if (cached is not null)
        {
            _logger.LogDebug("ModLog resolved channel from cache: #{Channel}", cached.Name);
            return cached;
        }

        // Fall back to REST if cache missed (e.g. bot just restarted)
        _logger.LogDebug("ModLog channel not in cache, falling back to REST for channel {ChannelId}", channelId);
        var restChannel = await _client.Rest.GetChannelAsync(channelId);

        if (restChannel is ITextChannel textChannel)
            return textChannel;

        if (restChannel is not null)
            _logger.LogWarning(
                "Channel {ChannelId} exists but is not a text channel (type: {Type})",
                channelId, restChannel.GetType().Name);

        return null;
    }
}