namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class GuildConfigRepository : BaseRepository<GuildConfig>, IGuildConfigRepository
{
    public GuildConfigRepository(AppDbContext context) : base(context) { }

    public async Task<GuildConfig?> GetByGuildIdAsync(ulong guildId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(g => g.GuildId == guildId, ct);

    public async Task<GuildConfig> GetOrCreateAsync(ulong guildId, string guildName, CancellationToken ct = default)
    {
        var config = await GetByGuildIdAsync(guildId, ct);
        if (config is not null) return config;

        config = new GuildConfig { GuildId = guildId, GuildName = guildName };
        await AddAsync(config, ct);
        await SaveChangesAsync(ct);
        return config;
    }
}
