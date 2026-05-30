namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WarningRepository : BaseRepository<Warning>, IWarningRepository
{
    public WarningRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Warning>> GetActiveWarningsAsync(
        ulong guildId, ulong userId, CancellationToken ct = default)
        => await DbSet
            .Where(w => w.GuildId == guildId && w.TargetUserId == userId && w.IsActive)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> GetWarningCountAsync(
        ulong guildId, ulong userId, CancellationToken ct = default)
        => await DbSet
            .CountAsync(w => w.GuildId == guildId && w.TargetUserId == userId && w.IsActive, ct);
}
