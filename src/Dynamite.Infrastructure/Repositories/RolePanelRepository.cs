// src/Dynamite.Infrastructure/Repositories/RolePanelRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class RolePanelRepository : BaseRepository<RolePanel>, IRolePanelRepository
{
    public RolePanelRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<RolePanel>> GetByGuildIdAsync(
        ulong guildId, CancellationToken ct = default)
        => await DbSet
            .Include(p => p.Items)
            .Where(p => p.GuildId == guildId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<RolePanel?> GetByMessageIdAsync(
        ulong guildId, ulong messageId, CancellationToken ct = default)
        => await DbSet
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.MessageId == messageId, ct);

    public async Task<RolePanelItem?> GetItemByIdAsync(
        Guid itemId, CancellationToken ct = default)
        => await Context.Set<RolePanelItem>()
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);
}