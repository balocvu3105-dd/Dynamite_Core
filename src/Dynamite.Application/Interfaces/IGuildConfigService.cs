namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;

public interface IGuildConfigService
{
    Task<GuildConfig> GetOrCreateConfigAsync(ulong guildId, string guildName, CancellationToken ct = default);
    Task UpdateConfigAsync(GuildConfig config, CancellationToken ct = default);
    Task SetModLogChannelAsync(ulong guildId, ulong channelId, CancellationToken ct = default);
}
