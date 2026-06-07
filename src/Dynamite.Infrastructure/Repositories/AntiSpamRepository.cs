// src/Dynamite.Infrastructure/Repositories/AntiSpamRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class AntiSpamRepository : BaseRepository<AntiSpamConfig>, IAntiSpamRepository
{
    public AntiSpamRepository(AppDbContext context) : base(context) { }

    public async Task<AntiSpamConfig?> GetByGuildIdAsync(
        ulong guildId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(a => a.GuildId == guildId, ct);

    public async Task<AntiSpamConfig> GetOrCreateAsync(
        ulong guildId, Guid guildConfigId, CancellationToken ct = default)
    {
        var existing = await GetByGuildIdAsync(guildId, ct);
        if (existing is not null) return existing;

        var config = new AntiSpamConfig
        {
            GuildId = guildId,
            GuildConfigId = guildConfigId
        };

        await AddAsync(config, ct);
        await SaveChangesAsync(ct);
        return config;
    }
}