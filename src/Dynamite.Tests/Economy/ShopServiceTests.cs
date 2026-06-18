// src/Dynamite.Tests/Economy/ShopServiceTests.cs
namespace Dynamite.Tests.Economy;

using Dynamite.Core.Common;
using Dynamite.Core.Common.Results;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class ShopServiceTests
{
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<IShopRepository>   _shopRepoMock;
    private readonly ShopService             _sut;

    private const ulong GuildId = 111111111111111111UL;
    private const ulong UserId  = 222222222222222222UL;

    public ShopServiceTests()
    {
        _walletRepoMock = new Mock<IWalletRepository>();
        _shopRepoMock   = new Mock<IShopRepository>();

        // FishBagService và WeatherService không dùng trong các test này
        _sut = new ShopService(
            _walletRepoMock.Object,
            _shopRepoMock.Object,
            null!,                                // FishBagService (not used in these tests)
            null!,                                // WeatherService (not used in these tests)
            NullLogger<ShopService>.Instance);
    }

    [Fact]
    public async Task Buy_ItemNotFound_ShouldFail()
    {
        // Arrange
        _shopRepoMock.Setup(r => r.GetItemByNameAsync(GuildId, "ghost")).ReturnsAsync((InventoryItem?)null);

        // Act
        var result = await _sut.BuyWithDetailsAsync(GuildId, UserId, "ghost");

        // Assert
        Assert.False(result);
        Assert.Contains("ghost", result.ErrorMessage); // "Không tìm thấy **ghost** trong cửa hàng."
    }

    [Fact]
    public async Task Buy_InsufficientBalance_ShouldFail()
    {
        // Arrange
        var item   = MakeItem(500);
        var wallet = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 100 };

        _shopRepoMock.Setup(r => r.GetItemByNameAsync(GuildId, item.Name)).ReturnsAsync(item);
        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var result = await _sut.BuyWithDetailsAsync(GuildId, UserId, item.Name);

        // Assert
        Assert.False(result);
        Assert.Equal(100, wallet.Coins); // không bị deduct
    }

    [Fact]
    public async Task Buy_SufficientBalance_ShouldDeductAndAddInventory()
    {
        // Arrange
        var item   = MakeItem(200);
        var wallet = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 500 };

        _shopRepoMock.Setup(r => r.GetItemByNameAsync(GuildId, item.Name)).ReturnsAsync(item);
        _shopRepoMock.Setup(r => r.GetUserItemAsync(wallet.Id, item.Id)).ReturnsAsync((UserInventory?)null);
        // BuyWithDetailsAsync gọi GetOrCreateAsync 2 lần (trước và sau buy)
        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var result = await _sut.BuyWithDetailsAsync(GuildId, UserId, item.Name);

        // Assert
        Assert.True(result);
        Assert.Equal(300, wallet.Coins); // 500 - 200
        Assert.Equal(200, result.Value!.CoinsPaid);
        _shopRepoMock.Verify(r => r.AddUserInventoryAsync(It.IsAny<UserInventory>()), Times.Once);
    }

    [Fact]
    public async Task Buy_FishingRodAlreadyOwned_ShouldFail()
    {
        // Arrange
        var item     = MakeItem(200, ItemType.FishingRod);
        var wallet   = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 500 };
        var existing = new UserInventory { WalletId = wallet.Id, ItemId = item.Id, Quantity = 1 };

        _shopRepoMock.Setup(r => r.GetItemByNameAsync(GuildId, item.Name)).ReturnsAsync(item);
        _shopRepoMock.Setup(r => r.GetUserItemAsync(wallet.Id, item.Id)).ReturnsAsync(existing);
        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var result = await _sut.BuyWithDetailsAsync(GuildId, UserId, item.Name);

        // Assert
        Assert.False(result);
    }

    // ── GetRepairCost ─────────────────────────────────────────────────────────

    [Fact]
    public void GetRepairCost_FullDamage_ShouldBeHalfPrice()
    {
        // 0/200 durability → cost = 50% giá
        var cost = ShopService.GetRepairCost(rodPrice: 20_000, maxDurability: 200, currentDurability: 0);
        Assert.Equal(10_000, cost);
    }

    [Fact]
    public void GetRepairCost_HalfDamage_ShouldBeQuarterPrice()
    {
        // 100/200 durability → cost = 25% giá
        var cost = ShopService.GetRepairCost(rodPrice: 20_000, maxDurability: 200, currentDurability: 100);
        Assert.Equal(5_000, cost);
    }

    [Fact]
    public void GetRepairCost_NoDamage_ShouldBeZero()
    {
        var cost = ShopService.GetRepairCost(rodPrice: 20_000, maxDurability: 200, currentDurability: 200);
        Assert.Equal(0, cost);
    }

    // ── RepairRodAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RepairRod_NoRods_ShouldFail()
    {
        var wallet = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 50_000 };
        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);
        _shopRepoMock.Setup(r => r.GetUserRodsAsync(wallet.Id)).ReturnsAsync([]);

        var result = await _sut.RepairRodAsync(GuildId, UserId, null);

        Assert.False(result);
        Assert.Contains("cần câu", result.ErrorMessage);
    }

    [Fact]
    public async Task RepairRod_AlreadyFullDurability_ShouldFail()
    {
        var item = MakeRod(price: 20_000, maxDur: 200);
        var wallet = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 50_000 };
        var inv = new UserInventory { Item = item, RodDurability = 200 };

        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);
        _shopRepoMock.Setup(r => r.GetUserRodsAsync(wallet.Id)).ReturnsAsync([inv]);

        var result = await _sut.RepairRodAsync(GuildId, UserId, null);

        Assert.False(result);
        Assert.Contains("nguyên vẹn", result.ErrorMessage);
    }

    [Fact]
    public async Task RepairRod_InsufficientCoins_ShouldFail()
    {
        var item = MakeRod(price: 20_000, maxDur: 200);
        var wallet = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 100 };
        var inv = new UserInventory { Item = item, RodDurability = 0 };

        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);
        _shopRepoMock.Setup(r => r.GetUserRodsAsync(wallet.Id)).ReturnsAsync([inv]);

        var result = await _sut.RepairRodAsync(GuildId, UserId, null);

        Assert.False(result);
        Assert.Equal(100, wallet.Coins); // không bị deduct
    }

    [Fact]
    public async Task RepairRod_BrokenRod_ShouldRestoreAndDeduct()
    {
        var item = MakeRod(price: 20_000, maxDur: 200);
        var wallet = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 50_000 };
        var inv = new UserInventory { Item = item, RodDurability = 0 };

        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);
        _shopRepoMock.Setup(r => r.GetUserRodsAsync(wallet.Id)).ReturnsAsync([inv]);

        var result = await _sut.RepairRodAsync(GuildId, UserId, null);

        Assert.True(result);
        Assert.Equal(10_000, result.Value!.CoinsPaid);     // 50% giá
        Assert.Equal(40_000, wallet.Coins);                // 50_000 - 10_000
        Assert.Equal(0,   result.Value.OldDurability);
        Assert.Equal(200, result.Value.NewDurability);
        Assert.Equal(200, inv.RodDurability);              // restored
    }

    [Fact]
    public async Task RepairRod_PartialDamage_ShouldChargeProportionally()
    {
        var item = MakeRod(price: 20_000, maxDur: 200);
        var wallet = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 50_000 };
        var inv = new UserInventory { Item = item, RodDurability = 100 }; // half damaged

        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);
        _shopRepoMock.Setup(r => r.GetUserRodsAsync(wallet.Id)).ReturnsAsync([inv]);

        var result = await _sut.RepairRodAsync(GuildId, UserId, null);

        Assert.True(result);
        Assert.Equal(5_000, result.Value!.CoinsPaid);  // 25% giá
        Assert.Equal(45_000, wallet.Coins);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static InventoryItem MakeItem(long price, ItemType type = ItemType.Collectible) => new()
    {
        Id          = Guid.NewGuid(),
        GuildId     = GuildId,
        Name        = "Test Item",
        Emoji       = "📦",
        Price       = price,
        Type        = type,
        IsAvailable = true
    };

    private static InventoryItem MakeRod(long price, int maxDur) => new()
    {
        Id            = Guid.NewGuid(),
        GuildId       = GuildId,
        Name          = "Test Rod",
        Emoji         = "🎣",
        Price         = price,
        Type          = ItemType.FishingRod,
        IsAvailable   = true,
        MaxDurability = maxDur,
    };
}
