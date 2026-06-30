// src/Dynamite.Infrastructure/Repositories/BlacklistRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class BlacklistRepository : BaseRepository<UserBlacklist>, IBlacklistRepository
{
    public BlacklistRepository(AppDbContext context) : base(context) { }

    public async Task<UserBlacklist?> GetActiveAsync(
        ulong guildId, ulong userId, CancellationToken ct = default)
        => await DbSet
            .Where(b => b.GuildId == guildId && b.TargetUserId == userId && b.IsActive)
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<UserBlacklist>> GetAllActiveAsync(
        ulong guildId, int count = 50, CancellationToken ct = default)
        => await DbSet
            .Where(b => b.GuildId == guildId && b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<bool> IsBlacklistedAsync(
        ulong guildId, ulong userId, CancellationToken ct = default)
        => await DbSet.AnyAsync(
            b => b.GuildId == guildId && b.TargetUserId == userId && b.IsActive, ct);
}
