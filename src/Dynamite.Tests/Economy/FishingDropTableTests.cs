// src/Dynamite.Tests/Economy/FishingDropTableTests.cs
namespace Dynamite.Tests.Economy;

using Dynamite.Modules.Economy.Helpers;
using Xunit;

public class FishingDropTableTests
{
    [Fact]
    public void Roll_ShouldAlwaysReturnPositiveCoins()
    {
        for (var i = 0; i < 100; i++)
        {
            var result = FishingDropTable.Roll();
            Assert.True(result.Coins > 0, $"Roll returned {result.Coins} coins");
        }
    }

    [Fact]
    public void Roll_WithMultiplier_ShouldReturnMoreCoinsOnAverage()
    {
        // Run 1000 rolls với multiplier 2.0 vs 1.0 và so sánh average
        var baseTotal = Enumerable.Range(0, 1000)
            .Sum(_ => FishingDropTable.Roll(1.0).Coins);

        var boostedTotal = Enumerable.Range(0, 1000)
            .Sum(_ => FishingDropTable.Roll(2.0).Coins);

        Assert.True(boostedTotal > baseTotal,
            $"Boosted ({boostedTotal}) should be greater than base ({baseTotal})");
    }

    [Fact]
    public void Roll_ShouldReturnKnownRarity()
    {
        var validRarities = new[] { "Common", "Uncommon", "Rare", "Legendary", "Mythic" };

        for (var i = 0; i < 50; i++)
        {
            var result = FishingDropTable.Roll();
            Assert.Contains(result.Rarity, validRarities);
        }
    }

    [Fact]
    public void Roll_ShouldReturnNonEmptyName()
    {
        var result = FishingDropTable.Roll();
        Assert.False(string.IsNullOrEmpty(result.Name));
    }
}