// src/Dynamite.Application/Services/WelcomeService.cs
namespace Dynamite.Application.Services;

using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class WelcomeService : IWelcomeService
{
    private readonly IGuildConfigRepository _guildConfigRepo;
    private readonly ILogger<WelcomeService> _logger;

    public WelcomeService(
        IGuildConfigRepository guildConfigRepo,
        ILogger<WelcomeService> logger)
    {
        _guildConfigRepo = guildConfigRepo;
        _logger = logger;
    }

    public async Task<GuildConfig?> GetWelcomeConfigAsync(
        ulong guildId, CancellationToken ct = default)
        => await _guildConfigRepo.GetByGuildIdAsync(guildId, ct);

    public async Task SetChannelAsync(
        ulong guildId, string guildName,
        ulong channelId, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        config.WelcomeChannelId = channelId;
        config.WelcomeEnabled = true;
        await SaveAsync(config, ct);
        _logger.LogInformation("Welcome channel set to {ChannelId} for guild {GuildId}", channelId, guildId);
    }

    public async Task SetMessageAsync(
        ulong guildId, string guildName,
        string message, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        config.WelcomeMessage = message;
        await SaveAsync(config, ct);
    }

    public async Task SetEnabledAsync(
        ulong guildId, string guildName,
        bool enabled, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        config.WelcomeEnabled = enabled;
        await SaveAsync(config, ct);
        _logger.LogInformation("Welcome {Status} for guild {GuildId}",
            enabled ? "enabled" : "disabled", guildId);
    }

    public async Task SetVerifyChannelAsync(
        ulong guildId, string guildName,
        ulong channelId, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        config.VerifyChannelId = channelId;
        await SaveAsync(config, ct);
    }

    public async Task SetVerifyRoleAsync(
        ulong guildId, string guildName,
        ulong roleId, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        config.VerifyRoleId = roleId;
        await SaveAsync(config, ct);
        _logger.LogInformation("Verify role set to {RoleId} for guild {GuildId}", roleId, guildId);
    }

    public async Task<(ulong? roleId, bool configured)> GetVerifyRoleAsync(
        ulong guildId, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetByGuildIdAsync(guildId, ct);
        if (config?.VerifyRoleId is null) return (null, false);
        return (config.VerifyRoleId, true);
    }

    private async Task SaveAsync(GuildConfig config, CancellationToken ct)
    {
        config.UpdatedAt = DateTime.UtcNow;
        await _guildConfigRepo.UpdateAsync(config, ct);
        await _guildConfigRepo.SaveChangesAsync(ct);
    }
}