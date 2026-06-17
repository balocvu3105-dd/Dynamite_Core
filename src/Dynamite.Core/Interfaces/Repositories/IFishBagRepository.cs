// src/Dynamite.Core/Interfaces/Repositories/IFishBagRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IFishBagRepository
{
    Task<UserFishBag> GetOrCreateAsync(ulong guildId, ulong userId);
    Task AddFishAsync(CaughtFish fish);
    Task RemoveFishAsync(IEnumerable<CaughtFish> fish);
    Task<List<CaughtFish>> GetFishByRarityAsync(Guid bagId, string rarity);
    Task<int> CountPearlsThisWeekAsync(ulong guildId, PearlType type);
    Task AddPearlLogAsync(GuildPearlLog log);
    Task SaveChangesAsync();
}
