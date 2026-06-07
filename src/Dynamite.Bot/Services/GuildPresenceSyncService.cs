// src/Dynamite.Bot/Services/GuildPresenceSyncService.cs
namespace Dynamite.Bot.Services;

using Discord.WebSocket;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class GuildPresenceSyncService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GuildPresenceSyncService> _logger;

    public GuildPresenceSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<GuildPresenceSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task SyncOnReadyAsync(DiscordSocketClient client)
    {
        // Cast IconId thành string? để match signature của SyncAllAsync
        var guilds = client.Guilds
            .Select(g => (GuildId: g.Id, GuildName: g.Name, IconHash: (string?)g.IconId))
            .ToList();

        _logger.LogInformation(
            "[GuildPresence] Syncing {Count} guilds on Ready", guilds.Count);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<IGuildPresenceRepository>();

        await repo.SyncAllAsync(guilds);

        _logger.LogInformation("[GuildPresence] Sync complete");
    }

    public async Task OnGuildJoinedAsync(SocketGuild guild)
    {
        _logger.LogInformation(
            "[GuildPresence] Bot joined guild {GuildId} ({GuildName})",
            guild.Id, guild.Name);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<IGuildPresenceRepository>();

        await repo.UpsertAsync(guild.Id, guild.Name, guild.IconId);
    }

    public async Task OnGuildLeftAsync(SocketGuild guild)
    {
        _logger.LogInformation(
            "[GuildPresence] Bot left guild {GuildId} ({GuildName})",
            guild.Id, guild.Name);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<IGuildPresenceRepository>();

        await repo.MarkAbsentAsync(guild.Id);
    }
}