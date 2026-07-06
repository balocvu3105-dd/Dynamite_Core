// src/Dynamite.Infrastructure/Repositories/ServerActivityLogRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class ServerActivityLogRepository : BaseRepository<ServerActivityLog>, IServerActivityLogRepository
{
    public ServerActivityLogRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<ServerActivityLog> Logs, int TotalCount)> GetLogsAsync(
        ulong guildId,
        LogCategory? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = Context.ServerActivityLogs.AsNoTracking().Where(l => l.GuildId == guildId);

        if (category.HasValue)
        {
            query = query.Where(l => l.Category == category.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(l =>
                l.Title.ToLower().Contains(s) ||
                l.Description.ToLower().Contains(s) ||
                l.EventType.ToLower().Contains(s) ||
                (l.ActorUsername != null && l.ActorUsername.ToLower().Contains(s)) ||
                (l.TargetUsername != null && l.TargetUsername.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync(ct);
        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (logs, totalCount);
    }
}
