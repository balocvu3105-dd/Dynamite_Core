namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class ModerationRepository : BaseRepository<ModerationAction>, IModerationRepository
{
    public ModerationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<ModerationAction>> GetUserHistoryAsync(
        ulong guildId, ulong userId, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.GuildId == guildId && a.TargetUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<ModerationAction>> GetRecentActionsAsync(
        ulong guildId, int count = 10, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.GuildId == guildId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
}
