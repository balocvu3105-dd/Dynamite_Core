namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class ModerationRepository : BaseRepository<ModerationAction>, IModerationRepository
{
    public ModerationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<ModerationAction>> GetUserHistoryAsync(
        ulong guildId, ulong userId,
        int count = 20, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.GuildId == guildId && a.TargetUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IEnumerable<ModerationAction>> GetByModeratorAsync(
        ulong guildId, ulong moderatorId,
        int count = 20, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.GuildId == guildId && a.ModeratorId == moderatorId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IEnumerable<ModerationAction>> GetRecentActionsAsync(
        ulong guildId, int count = 10,
        ModerationActionType? type = null,
        CancellationToken ct = default)
    {
        var query = DbSet.Where(a => a.GuildId == guildId);
        if (type.HasValue)
            query = query.Where(a => a.ActionType == type.Value);
        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }
}
