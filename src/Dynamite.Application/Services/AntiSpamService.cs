// src/Dynamite.Application/Services/AntiSpamService.cs
namespace Dynamite.Application.Services;

using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class AntiSpamService : IAntiSpamService
{
    private readonly IAntiSpamRepository _antiSpamRepo;
    private readonly IGuildConfigRepository _guildConfigRepo;
    private readonly ILogger<AntiSpamService> _logger;

    public AntiSpamService(
        IAntiSpamRepository antiSpamRepo,
        IGuildConfigRepository guildConfigRepo,
        ILogger<AntiSpamService> logger)
    {
        _antiSpamRepo = antiSpamRepo;
        _guildConfigRepo = guildConfigRepo;
        _logger = logger;
    }

    public async Task<AntiSpamConfig?> GetConfigAsync(
        ulong guildId, CancellationToken ct = default)
        => await _antiSpamRepo.GetByGuildIdAsync(guildId, ct);

    public async Task<AntiSpamConfig> GetOrCreateConfigAsync(
        ulong guildId, string guildName, CancellationToken ct = default)
    {
        var guildConfig = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        return await _antiSpamRepo.GetOrCreateAsync(guildId, guildConfig.Id, ct);
    }

    public async Task SetEnabledAsync(
        ulong guildId, string guildName, bool enabled, CancellationToken ct = default)
    {
        var config = await GetOrCreateConfigAsync(guildId, guildName, ct);
        config.Enabled = enabled;
        await SaveAsync(config, ct);
        _logger.LogInformation("AntiSpam {Status} for guild {GuildId}",
            enabled ? "enabled" : "disabled", guildId);
    }

    public async Task SetMessageThresholdAsync(
        ulong guildId, string guildName,
        int threshold, int windowSeconds, CancellationToken ct = default)
    {
        if (threshold < 1 || threshold > 30)
            throw new ArgumentException("Threshold must be between 1 and 30.");
        if (windowSeconds < 1 || windowSeconds > 60)
            throw new ArgumentException("Window must be between 1 and 60 seconds.");

        var config = await GetOrCreateConfigAsync(guildId, guildName, ct);
        config.MessageThreshold = threshold;
        config.MessageWindowSeconds = windowSeconds;
        await SaveAsync(config, ct);
    }

    public async Task SetMentionThresholdAsync(
        ulong guildId, string guildName,
        int threshold, CancellationToken ct = default)
    {
        if (threshold < 1 || threshold > 20)
            throw new ArgumentException("Mention threshold must be between 1 and 20.");

        var config = await GetOrCreateConfigAsync(guildId, guildName, ct);
        config.MentionThreshold = threshold;
        await SaveAsync(config, ct);
    }

    public async Task SetFeatureAsync(
        ulong guildId, string guildName,
        string feature, bool enabled, CancellationToken ct = default)
    {
        var config = await GetOrCreateConfigAsync(guildId, guildName, ct);

        switch (feature.ToLower())
        {
            case "antiinvite": config.AntiInvite = enabled; break;
            case "antiscam":
            case "antiscamlink": config.AntiScamLink = enabled; break;
            case "antiraid": config.AntiRaid = enabled; break;
            default: throw new ArgumentException($"Unknown feature: {feature}");
        }

        await SaveAsync(config, ct);
        _logger.LogInformation("Feature {Feature} {Status} for guild {GuildId}",
            feature, enabled ? "enabled" : "disabled", guildId);
    }

    public async Task SetRaidThresholdAsync(
        ulong guildId, string guildName,
        int threshold, CancellationToken ct = default)
    {
        if (threshold < 3 || threshold > 50)
            throw new ArgumentException("Raid threshold must be between 3 and 50.");

        var config = await GetOrCreateConfigAsync(guildId, guildName, ct);
        config.RaidThreshold = threshold;
        await SaveAsync(config, ct);
    }

    private async Task SaveAsync(AntiSpamConfig config, CancellationToken ct)
    {
        config.UpdatedAt = DateTime.UtcNow;
        await _antiSpamRepo.UpdateAsync(config, ct);
        await _antiSpamRepo.SaveChangesAsync(ct);
    }
}