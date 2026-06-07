// src/Dynamite.Application/Interfaces/IAntiSpamService.cs
namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;

public interface IAntiSpamService
{
    Task<AntiSpamConfig?> GetConfigAsync(ulong guildId, CancellationToken ct = default);

    Task<AntiSpamConfig> GetOrCreateConfigAsync(
        ulong guildId, string guildName, CancellationToken ct = default);

    Task SetEnabledAsync(ulong guildId, string guildName,
        bool enabled, CancellationToken ct = default);

    Task SetMessageThresholdAsync(ulong guildId, string guildName,
        int threshold, int windowSeconds, CancellationToken ct = default);

    Task SetMentionThresholdAsync(ulong guildId, string guildName,
        int threshold, CancellationToken ct = default);

    Task SetFeatureAsync(ulong guildId, string guildName,
        string feature, bool enabled, CancellationToken ct = default);

    Task SetRaidThresholdAsync(ulong guildId, string guildName,
        int threshold, CancellationToken ct = default);
}