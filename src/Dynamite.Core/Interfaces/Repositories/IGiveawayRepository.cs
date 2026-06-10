// src/Dynamite.Core/Interfaces/Repositories/IGiveawayRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IGiveawayRepository
{
    Task<Giveaway?> GetByIdAsync(Guid id);
    Task<Giveaway?> GetByMessageIdAsync(ulong messageId);
    Task<List<Giveaway>> GetActiveGiveawaysAsync();
    Task<List<Giveaway>> GetExpiredUnendedAsync();
    Task AddAsync(Giveaway giveaway);
    Task AddEntryAsync(GiveawayEntry entry);
    Task<bool> HasEnteredAsync(Guid giveawayId, ulong userId);
    Task<List<GiveawayEntry>> GetEntriesAsync(Guid giveawayId);
    Task<int> GetEntryCountAsync(Guid giveawayId);
    Task SaveChangesAsync();
}