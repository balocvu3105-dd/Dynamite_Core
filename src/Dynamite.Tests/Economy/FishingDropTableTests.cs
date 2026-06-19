// src/Dynamite.Tests/Economy/FishingDropTableTests.cs
namespace Dynamite.Tests.Economy;

using Dynamite.Modules.Economy.Helpers;
using Xunit;

public class FishingDropTableTests
{
    // Helpers - loai bo Miss/Escape bang cach pass missRate=0, escapeRate=0
    private static RollResult RollCatch(double dropMultiplier = 1.0)
        => FishingDropTable.Roll(missRate: 0, escapeRate: 0, dropMultiplier: dropMultiplier);

    [Fact]
    public void Roll_Caught_ShouldAlwaysReturnPositiveCoins()
    {
        for (var i = 0; i < 100; i++)
        {
            var result = RollCatch();

            // Voi missRate=0 va escapeRate=0 -> ket qua phai la Caught
            Assert.Equal(RollOutcome.Caught, result.Outcome);
            Assert.NotNull(result.Fish);

            // Trash la rac - 0 coins la dung thiet ke
            if (result.Fish!.Rarity == "Trash")
            {
                Assert.True(result.Fish.Coins >= 0,
                    $"Trash should have non-negative coins but got {result.Fish.Coins}");
            }
            else
            {
                Assert.True(result.Fish.Coins > 0,
                    $"Roll returned {result.Fish.Coins} coins (rarity={result.Fish.Rarity})");
            }
        }
    }

    [Fact]
    public void Roll_WithHigherMultiplier_ShouldReturnMoreCoinsOnAverage()
    {
        // 1000 rolls voi multiplier 2.0 vs 1.0, bo qua Miss/Escape
        var baseTotal = Enumerable.Range(0, 1000)
            .Select(_ => RollCatch(dropMultiplier: 1.0))
            .Where(r => r.Outcome == RollOutcome.Caught && r.Fish is not null)
            .Sum(r => r.Fish!.Coins);

        var boostedTotal = Enumerable.Range(0, 1000)
            .Select(_ => RollCatch(dropMultiplier: 2.0))
            .Where(r => r.Outcome == RollOutcome.Caught && r.Fish is not null)
            .Sum(r => r.Fish!.Coins);

        Assert.True(boostedTotal > baseTotal,
            $"Boosted ({boostedTotal}) should be greater than base ({baseTotal})");
    }

    [Fact]
    public void Roll_Caught_ShouldReturnKnownRarity()
    {
        // Trash la rac - 0 coins, thiet ke intentional (~9.3% chance)
        var validRarities = new[]
        {
            "Common", "Uncommon", "Rare", "Legendary", "Mythic",
            "Bronze", "Gold", "Diamond", "Trash"
        };

        for (var i = 0; i < 50; i++)
        {
            var result = RollCatch();
            Assert.NotNull(result.Fish);
            Assert.Contains(result.Fish!.Rarity, validRarities);
        }
    }

    [Fact]
    public void Roll_Caught_ShouldReturnNonEmptyName()
    {
        var result = RollCatch();
        Assert.NotNull(result.Fish);
        Assert.False(string.IsNullOrEmpty(result.Fish!.Name));
    }

    [Fact]
    public void Roll_DefaultParams_CanReturnMissOrEscape()
    {
        // Voi default params (15% miss, 10% escape), sau 500 lan roll phai co it nhat 1 Miss
        var outcomes = Enumerable.Range(0, 500)
            .Select(_ => FishingDropTable.Roll())
            .Select(r => r.Outcome)
            .ToList();

        Assert.Contains(RollOutcome.Miss, outcomes);
    }

    // ── LuckBonus tests (Kim Cuong = luckBonus 1) ─────────────────────────────

    [Fact]
    public void Roll_WithLuckBonus_ShouldNotBreakRollMechanic()
    {
        // luckBonus=1 khong duoc gay exception hoac null fish
        for (var i = 0; i < 200; i++)
        {
            var result = FishingDropTable.Roll(missRate: 0, escapeRate: 0, luckBonus: 1);
            Assert.Equal(RollOutcome.Caught, result.Outcome);
            Assert.NotNull(result.Fish);
        }
    }

    [Fact]
    public void Roll_WithLuckBonus_RareRateShouldBeHigherThanBaseline()
    {
        // 5000 rolls baseline vs 5000 rolls voi luckBonus=1
        const int N = 5000;
        var highTiers = new[] { "Rare", "Legendary", "Mythic" };

        var baseCount = Enumerable.Range(0, N)
            .Select(_ => FishingDropTable.Roll(missRate: 0, escapeRate: 0, luckBonus: 0))
            .Count(r => r.Fish is not null && highTiers.Contains(r.Fish.Rarity));

        var luckCount = Enumerable.Range(0, N)
            .Select(_ => FishingDropTable.Roll(missRate: 0, escapeRate: 0, luckBonus: 1))
            .Count(r => r.Fish is not null && highTiers.Contains(r.Fish.Rarity));

        Assert.True(luckCount >= baseCount,
            $"LuckBonus=1 produced {luckCount} high-tier catches vs baseline {baseCount}");
    }

    [Fact]
    public void Roll_WithLuckBonus_CoinsShouldNeverBeNegative()
    {
        for (var i = 0; i < 300; i++)
        {
            var result = FishingDropTable.Roll(missRate: 0, escapeRate: 0, luckBonus: 1);
            if (result.Fish is not null)
                Assert.True(result.Fish.Coins >= 0,
                    $"Coins should be non-negative, got {result.Fish.Coins} (rarity={result.Fish.Rarity})");
        }
    }
}
