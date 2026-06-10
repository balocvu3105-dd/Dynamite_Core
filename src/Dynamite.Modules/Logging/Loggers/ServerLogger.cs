// src/Dynamite.Modules/Logging/Loggers/ServerLogger.cs
namespace Dynamite.Modules.Logging.Loggers;
using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Dynamite.Modules.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public class ServerLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<ServerLogger> _logger;
    public ServerLogger(
        IServiceScopeFactory scopeFactory,
        DiscordSocketClient client,
        ILogger<ServerLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _logger = logger;
    }
    public async Task OnChannelCreatedAsync(SocketChannel channel)
    {
        if (channel is not SocketGuildChannel guildChannel) return;
        var type = GetChannelType(channel);

        var embed = LogEmbedHelper.ChannelCreated(guildChannel.Name, type);
        await SendLogAsync(guildChannel.Guild.Id, LogCategory.Server, embed);

        var auditEmbed = LogEmbedHelper.ChannelCreatedAudit(guildChannel.Name, type, guildChannel.Id);
        await SendLogAsync(guildChannel.Guild.Id, LogCategory.Audit, auditEmbed);
    }
    public async Task OnChannelDestroyedAsync(SocketChannel channel)
    {
        if (channel is not SocketGuildChannel guildChannel) return;
        var type = GetChannelType(channel);

        var embed = LogEmbedHelper.ChannelDeleted(guildChannel.Name, type);
        await SendLogAsync(guildChannel.Guild.Id, LogCategory.Server, embed);

        var auditEmbed = LogEmbedHelper.ChannelDeletedAudit(guildChannel.Name, type, guildChannel.Id);
        await SendLogAsync(guildChannel.Guild.Id, LogCategory.Audit, auditEmbed);
    }
    public async Task OnRoleCreatedAsync(SocketRole role)
    {
        var embed = LogEmbedHelper.RoleCreated(role.Name);
        await SendLogAsync(role.Guild.Id, LogCategory.Server, embed);

        var auditEmbed = LogEmbedHelper.RoleCreatedAudit(role.Name, role.Id);
        await SendLogAsync(role.Guild.Id, LogCategory.Audit, auditEmbed);
    }
    public async Task OnRoleDeletedAsync(SocketRole role)
    {
        var embed = LogEmbedHelper.RoleDeleted(role.Name);
        await SendLogAsync(role.Guild.Id, LogCategory.Server, embed);

        var auditEmbed = LogEmbedHelper.RoleDeletedAudit(role.Name, role.Id);
        await SendLogAsync(role.Guild.Id, LogCategory.Audit, auditEmbed);
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
                _logger.LogWarning("Server log channel {ChannelId} not found in guild {GuildId}", channelId, guildId);
                return;
            }
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send server log to channel {ChannelId}", channelId);
        }
    }
    private static string GetChannelType(SocketChannel channel)
    {
        if (channel is SocketForumChannel) return "Forum";
        if (channel is SocketVoiceChannel) return "Voice";
        if (channel is SocketCategoryChannel) return "Category";
        if (channel is SocketTextChannel) return "Text";
        return "Unknown";
    }
}