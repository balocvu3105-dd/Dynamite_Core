// src/Dynamite.Tests/Economy/FishingDropTableTests.cs
namespace Dynamite.Tests.Economy;

using Dynamite.Modules.Economy.Helpers;
using Xunit;

public class FishingDropTableTests
{
    // Helpers — loại bỏ Miss/Escape bằng cách pass missRate=0, escapeRate=0
    private static RollResult RollCatch(double dropMultiplier = 1.0)
        => FishingDropTable.Roll(missRate: 0, escapeRate: 0, dropMultiplier: dropMultiplier);

    [Fact]
    public void Roll_Caught_ShouldAlwaysReturnPositiveCoins()
    {
        for (var i = 0; i < 100; i++)
        {
            var result = RollCatch();

            // Với missRate=0 và escapeRate=0 → kết quả phải là Caught
            Assert.Equal(RollOutcome.Caught, result.Outcome);
            Assert.NotNull(result.Fish);

            // Trash là rác — 0 coins là đúng thiết kế
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
        // 1000 rolls với multiplier 2.0 vs 1.0, bỏ qua Miss/Escape
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
        // Bao gồm chest rarities vì chest check xảy ra trước fish roll
        // Trash là rác — 0 coins, thiết kế intentional (~9.3% chance)
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
        Assert.False(string.IsNullOrEmpty(re