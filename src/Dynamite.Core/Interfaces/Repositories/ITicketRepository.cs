// src/Dynamite.Core/Interfaces/Repositories/ITicketRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface ITicketRepository
{
    Task<TicketConfig?> GetConfigAsync(ulong guildId);
    Task<Ticket?> GetByChannelIdAsync(ulong channelId);
    Task<Ticket?> GetOpenTicketByOwnerAsync(ulong guildId, ulong ownerId);
    Task AddConfigAsync(TicketConfig config);
    Task AddTicketAsync(Ticket ticket);
    Task SaveChangesAsync();
}