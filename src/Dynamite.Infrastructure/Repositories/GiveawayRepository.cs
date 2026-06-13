// src/Dynamite.Infrastructure/Repositories/GiveawayRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class GiveawayRepository : IGiveawayRepository
{
    private readonly AppDbContext _db;

    public GiveawayRepository(AppDbContext db) => _db = db;

    public Task<Giveaway?> GetByIdAsync(Guid id)
        => _db.Giveaways.Include(g => g.Entries).FirstOrDefaultAsync(g => g.Id == id);

    public Task<Giveaway?> GetByMessageIdAsync(ulong messageId)
        => _db.Giveaways
            .Include(g => g.Entries)
            .FirstOrDefaultAsync(g => g.MessageId == messageId);

    public Task<List<Giveaway>> GetActiveGiveawaysAsync()
        => _db.Giveaways
            .Where(g => !g.IsEnded && !g.IsCancelled)
            .ToListAsync();

    public Task<List<Giveaway>> GetActiveByGuildAsync(ulong guildId)
        => _db.Giveaways
            .Where(g => g.GuildId == guildId && !g.IsEnded && !g.IsCancelled)
            .OrderBy(g => g.EndsAt)
            .ToListAsync();

    public Task<List<Giveaway>> GetExpiredUnendedAsync()
        => _db.Giveaways
            .Where(g => !g.IsEnded && !g.IsCancelled && g.EndsAt <= DateTime.UtcNow)
            .ToListAsync();

    public async Task AddAsync(Giveaway giveaway)
        => await _db.Giveaways.AddAsync(giveaway);

    public async Task AddEntryAsync(GiveawayEntry entry)
        => await _db.GiveawayEntries.AddAsync(entry);

    public Task<bool> HasEnteredAsync(Guid giveawayId, ulong userId)
        => _db.GiveawayEntries.AnyAsync(e => e.GiveawayId == giveawayId && e.UserId == userId);

    public Task<List<GiveawayEntry>> GetEntriesAsync(Guid giveawayId)
        => _db.GiveawayEntries.Where(e => e.GiveawayId == giveawayId).ToListAsync();

    public Task<int> GetEntryCountAsync(Guid giveawayId)
        => _db.GiveawayEntries.CountAsync(e => e.GiveawayId == giveawayId);

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}