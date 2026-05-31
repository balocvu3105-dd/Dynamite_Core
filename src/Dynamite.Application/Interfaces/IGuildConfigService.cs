namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;

public interface IGuildConfigService
{
    Task<GuildConfig> GetOrCreateConfigAsync(ulong guildId, string guildName, CancellationToken ct = default);
    Task UpdateConfigAsync(GuildConfig config, CancellationToken ct = default);

    // Fix: require guildName so GetOrCreateAsync doesn't store the guild ID string
    // as the guild name when a config doesn't exist yet.
    Task SetModLogChannelAsync(ulong guildId, string guildName, ulong channelId, CancellationToken ct = default);
}