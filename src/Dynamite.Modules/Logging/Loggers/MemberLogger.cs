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
        var embed = LogEmbedHelper.MemberJoined(user.ToString(), user.Id, user.CreatedAt);
        await SendLogAsync(user.Guild.Id, LogCategory.Member, embed);

        var auditEmbed = LogEmbedHelper.MemberJoinedAudit(user.ToString(), user.Id, user.CreatedAt);
        await SendLogAsync(user.Guild.Id, LogCategory.Audit, auditEmbed);
    }
    public async Task OnUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        var embed = LogEmbedHelper.MemberLeft(user.ToString(), user.Id);
        await SendLogAsync(guild.Id, LogCategory.Member, embed);

        var auditEmbed = LogEmbedHelper.MemberLeftAudit(user.ToString(), user.Id);
        await SendLogAsync(guild.Id, LogCategory.Audit, auditEmbed);
    }
    public async Task OnGuildMemberUpdatedAsync(
        Cacheable<SocketGuildUser, ulong> before,
        SocketGuildUser after)
    {
        if (!before.HasValue) return;
        var beforeUser = before.Value;

        if (beforeUser.Nickname != after.Nickname)
        {
            var embed = LogEmbedHelper.NicknameChanged(after.ToString(), after.Id, beforeUser.Nickname, after.Nickname);
            await SendLogAsync(after.Guild.Id, LogCategory.Member, embed);

            var auditEmbed = LogEmbedHelper.NicknameChangedAudit(after.ToString(), after.Id, beforeUser.Nickname, after.Nickname);
            await SendLogAsync(after.Guild.Id, LogCategory.Audit, auditEmbed);
        }

        var addedRoles = after.Roles.Except(beforeUser.Roles).ToList();
        var removedRoles = beforeUser.Roles.Except(after.Roles).ToList();

        foreach (var role in addedRoles)
        {
            var embed = LogEmbedHelper.RoleAdded(after.ToString(), after.Id, role.Name);
            await SendLogAsync(after.Guild.Id, LogCategory.Member, embed);

            var auditEmbed = LogEmbedHelper.RoleAddedAudit(after.ToString(), after.Id, role.Name, role.Id);
            await SendLogAsync(after.Guild.Id, LogCategory.Audit, auditEmbed);
        }

        foreach (var role in removedRoles)
        {
            var embed = LogEmbedHelper.RoleRemoved(after.ToString(), after.Id, role.Name);
            await SendLogAsync(after.Guild.Id, LogCategory.Member, embed);

            var auditEmbed = LogEmbedHelper.RoleRemovedAudit(after.ToString(), after.Id, role.Name, role.Id);
            await SendLogAsync(after.Guild.Id, LogCategory.Audit, auditEmbed);
        }
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