// src/Dynamite.Infrastructure/Repositories/FishEncyclopediaRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class FishEncyclopediaRepository : IFishEncyclopediaRepository
{
    // Thứ tự hiển thị rarity trong /fishing dex
    private static readonly string[] RarityOrder =
    [
        "Mythic", "Legendary", "Rare", "Uncommon", "Common",
        "Diamond", "Gold", "Bronze", "Trash"
    ];

    private readonly AppDbContext _db;
    public FishEncyclopediaRepository(AppDbContext db) => _db = db;

    public async Task UpsertAsync(
        ulong guildId, ulong userId,
        string fishName, string emoji, string rarity,
        long coins)
    {
        var entry = await _db.FishEncyclopedia
            .FirstOrDefaultAsync(e =>
                e.GuildId  == guildId  &&
                e.UserId   == userId   &&
                e.FishName == fishName);

        if (entry is null)
        {
            await _db.FishEncyclopedia.AddAsync(new FishEncyclopediaEntry
            {
                GuildId      = guildId,
                UserId       = userId,
                FishName     = fishName,
                Emoji        = emoji,
                Rarity       = rarity,
                TimesCaught  = 1,
                BestCoins    = coins,
                FirstCaughtAt = DateTime.UtcNow,
                LastCaughtAt  = DateTime.UtcNow,
            });
        }
        else
        {
            entry.TimesCaught++;
            entry.LastCaughtAt = DateTime.UtcNow;
            if (coins > entry.BestCoins) entry.BestCoins = coins;
        }
    }

    public Task<List<FishEncyclopediaEntry>> GetUserDexAsync(ulong guildId, ulong userId)
        => _db.FishEncyclopedia
            .Where(e => e.GuildId == guildId && e.UserId == userId)
            .ToListAsync();

    public Task<int> CountSpeciesAsync(ulong guildId, ulong userId)
        => _db.FishEncyclopedia
            .CountAsync(e => e.GuildId == guildId && e.UserId == userId);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
