// src/Dynamite.Infrastructure/Repositories/AutoRoleRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class AutoRoleRepository : BaseRepository<AutoRoleConfig>, IAutoRoleRepository
{
    public AutoRoleRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<AutoRoleConfig>> GetByGuildIdAsync(
        ulong guildId, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.GuildId == guildId)
            .ToListAsync(ct);

    public async Task<AutoRoleConfig?> GetByGuildAndRoleAsync(
        ulong guildId, ulong roleId, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(a => a.GuildId == guildId && a.RoleId == roleId, ct);
}