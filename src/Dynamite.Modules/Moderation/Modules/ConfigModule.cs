namespace Dynamite.Modules.Moderation.Modules;

using Discord;
using Discord.Interactions;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Moderation.Helpers;
using Microsoft.Extensions.Logging;

[RequireContext(ContextType.Guild)]
public class ConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IGuildConfigService _configService;
    private readonly ILogger<ConfigModule> _logger;

    public ConfigModule(IGuildConfigService configService, ILogger<ConfigModule> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    [SlashCommand("setmodlog", "Set the channel for moderation logs")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SetModLogAsync(
        [Summary("channel", "The channel to send mod logs to")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            // Fix: pass guild name so the config record stores the real name
            // rather than the ID string if the guild hasn't been seen before.
            await _configService.SetModLogChannelAsync(
                Context.Guild.Id,
                Context.Guild.Name,
                channel.Id);

            await FollowupAsync(embed: EmbedHelper.Success(
                "Mod log set",
                $"Moderation logs will now be sent to {channel.Mention}."), ephemeral: true);

            _logger.LogInformation("Mod log channel set to {ChannelId} for guild {GuildId}",
                channel.Id, Context.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting mod log channel for guild {GuildId}", Context.Guild.Id);
            await FollowupAsync(embed: EmbedHelper.Error("Error", "An unexpected error occurred."), ephemeral: true);
        }
    }
}