// src/Dynamite.Modules/Logging/Loggers/MemberLogger.cs
namespace Dynamite.Modules.Logging.Loggers;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Dynamite.Modules.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class MemberLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<MemberLogger> _logger;

    public MemberLogger(
        IServiceScopeFactory scopeFactory,
        DiscordSocketClient client,
        ILogger<MemberLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _logger = logger;
    }

    public async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var embed = LogEmbedHelper.MemberJoined(
            user.ToString(), user.Id, user.CreatedAt);
        await SendLogAsync(user.Guild.Id, embed);
    }

    public async Task OnUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        var embed = LogEmbedHelper.MemberLeft(user.ToString(), user.Id);
        await SendLogAsync(guild.Id, embed);
    }

    public async Task OnGuildMemberUpdatedAsync(
        Cacheable<SocketGuildUser, ulong> before,
        SocketGuildUser after)
    {
        if (!before.HasValue) return;
        var beforeUser = before.Value;

        if (beforeUser.Nickname != after.Nickname)
        {
            var embed = LogEmbedHelper.NicknameChanged(
                after.ToString(), after.Id,
                beforeUser.Nickname, after.Nickname);
            await SendLogAsync(after.Guild.Id, embed);
        }

        var addedRoles = after.Roles.Except(beforeUser.Roles).ToList();
        var removedRoles = beforeUser.Roles.Except(after.Roles).ToList();

        foreach (var role in addedRoles)
        {
            var embed = LogEmbedHelper.RoleAdded(after.ToString(), after.Id, role.Name);
            await SendLogAsync(after.Guild.Id, embed);
        }

        foreach (var role in removedRoles)
        {
            var embed = LogEmbedHelper.RoleRemoved(after.ToString(), after.Id, role.Name);
            await SendLogAsync(after.Guild.Id, embed);
        }
    }

    private async Task SendLogAsync(ulong guildId, Embed embed)
    {
        using var scope = _scopeFactory.CreateScope();
        var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();

        var channelId = await logService.GetLogChannelAsync(guildId, LogCategory.Member);
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
                _logger.LogWarning("Member log channel {ChannelId} not found in guild {GuildId}", channelId, guildId);
                return;
            }
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send member log to channel {ChannelId}", channelId);
        }
    }
}