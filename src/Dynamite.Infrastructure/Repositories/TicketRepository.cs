// src/Dynamite.Infrastructure/Repositories/TicketRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketRepository(AppDbContext db) => _db = db;

    public Task<TicketConfig?> GetConfigAsync(ulong guildId)
        => _db.TicketConfigs
            .FirstOrDefaultAsync(c => c.GuildId == guildId);

    public Task<Ticket?> GetByChannelIdAsync(ulong channelId)
        => _db.Tickets
            .Include(t => t.TicketConfig)
            .FirstOrDefaultAsync(t => t.ChannelId == channelId);

    public Task<Ticket?> GetOpenTicketByOwnerAsync(ulong guildId, ulong ownerId)
        => _db.Tickets
            .FirstOrDefaultAsync(t =>
                t.GuildId == guildId &&
                t.OwnerId == ownerId &&
                t.Status == TicketStatus.Open);

    public async Task AddConfigAsync(TicketConfig config)
        => await _db.TicketConfigs.AddAsync(config);

    public async Task AddTicketAsync(Ticket ticket)
        => await _db.Tickets.AddAsync(ticket);

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}