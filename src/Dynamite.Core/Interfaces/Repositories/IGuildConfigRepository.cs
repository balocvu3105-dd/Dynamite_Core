namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IGuildConfigRepository : IRepository<GuildConfig>
{
    Task<GuildConfig?> GetByGuildIdAsync(ulong guildId, CancellationToken ct = default);
    Task<GuildConfig> GetOrCreateAsync(ulong guildId, string guildName, CancellationToken ct = default);
}