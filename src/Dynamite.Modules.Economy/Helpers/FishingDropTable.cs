// src/Dynamite.Modules.Economy/Helpers/FishingDropTable.cs
namespace Dynamite.Modules.Economy.Helpers;

public record FishCatch(string Name, string Emoji, long Coins, string Rarity);

public static class FishingDropTable
{
    private static readonly Random _rng = new();

    private static readonly List<(int weight, FishCatch fish)> _table =
    [
        (50, new("Common Fish",    "🐟", 0,   "Common")),
        (30, new("Uncommon Fish",  "🐠", 0,   "Uncommon")),
        (15, new("Rare Fish",      "🐡", 0,   "Rare")),
        (4,  new("Legendary Fish", "🦈", 0,   "Legendary")),
        (1,  new("Treasure Chest", "💰", 0,   "Mythic")),
    ];

    private static readonly Dictionary<string, (long min, long max)> _coinRanges = new()
    {
        ["Common"]    = (10,  30),
        ["Uncommon"]  = (40,  80),
        ["Rare"]      = (100, 200),
        ["Legendary"] = (300, 500),
        ["Mythic"]    = (600, 1000),
    };

    public static FishCatch Roll(double multiplier = 1.0)
    {
        var totalWeight = _table.Sum(t => t.weight);
        var roll = _rng.Next(totalWeight);
        var cumulative = 0;

        foreach (var (weight, fish) in _table)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                var (min, max) = _coinRanges[fish.Rarity];
                var coins = (long)((_rng.NextInt64(min, max + 1)) * multiplier);
                return fish with { Coins = coins };
            }
        }

        // Fallback
        var (fMin, fMax) = _coinRanges["Common"];
        return _table[0].fish with { Coins = _rng.NextInt64(fMin, fMax + 1) };
    }
}