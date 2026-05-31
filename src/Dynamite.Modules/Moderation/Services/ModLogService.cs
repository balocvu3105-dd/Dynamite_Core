namespace Dynamite.Modules.Moderation.Services;

using Discord;
using Discord.Rest;
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
                _logger.LogInformation("ModLog skipped — no channel configured");
                return;
            }

            var guild = _client.GetGuild(guildId);
            _logger.LogInformation("ModLog guild cache — {Guild}", guild?.Name ?? "null");

            var channel = guild?.GetTextChannel(config.ModLogChannelId.Value)
                          ?? (ITextChannel?)await _client.Rest.GetChannelAsync(config.ModLogChannelId.Value);

            _logger.LogInformation("ModLog channel — {Channel}", channel?.Name ?? "null");

            if (channel is null)
            {
                _logger.LogWarning("Mod log channel {ChannelId} not found", config.ModLogChannelId.Value);
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
}