namespace Dynamite.Application.Services;

using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class GuildConfigService : IGuildConfigService
{
    private readonly IGuildConfigRepository _guildConfigRepo;
    private readonly ILogger<GuildConfigService> _logger;

    public GuildConfigService(
        IGuildConfigRepository guildConfigRepo,
        ILogger<GuildConfigService> logger)
    {
        _guildConfigRepo = guildConfigRepo;
        _logger = logger;
    }

    public async Task<GuildConfig> GetOrCreateConfigAsync(
        ulong guildId, string guildName, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        _logger.LogDebug("Loaded config for guild {GuildId}", guildId);
        return config;
    }

    public async Task UpdateConfigAsync(GuildConfig config, CancellationToken ct = default)
    {
        config.UpdatedAt = DateTime.UtcNow;
        await _guildConfigRepo.UpdateAsync(config, ct);
        await _guildConfigRepo.SaveChangesAsync(ct);
    }

   public async Task SetModLogChannelAsync(ulong guildId, ulong channelId, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildId.ToString(), ct);
        config.ModLogChannelId = channelId;
        await UpdateConfigAsync(config, ct);
    }
}
