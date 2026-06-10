// src/Dynamite.Tests/Economy/ShopServiceTests.cs
namespace Dynamite.Tests.Economy;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class ShopServiceTests
{
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<IShopRepository> _shopRepoMock;
    private readonly ShopService _sut;

    private const ulong GuildId = 111111111111111111UL;
    private const ulong UserId  = 222222222222222222UL;

    public ShopServiceTests()
    {
        _walletRepoMock = new Mock<IWalletRepository>();
        _shopRepoMock   = new Mock<IShopRepository>();
        _sut = new ShopService(
            _walletRepoMock.Object,
            _shopRepoMock.Object,
            NullLogger<ShopService>.Instance);
    }

    [Fact]
    public async Task Buy_ItemNotFound_ShouldFail()
    {
        // Arrange
        _shopRepoMock.Setup(r => r.GetItemByNameAsync(GuildId, "ghost")).ReturnsAsync((InventoryItem?)null);

        // Act
        var (success, message) = await _sut.BuyAsync(GuildId, UserId, "ghost");

        // Assert
        Assert.False(success);
        Assert.Contains("not found", message);
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
        var (success, message) = await _sut.BuyAsync(GuildId, UserId, item.Name);

        // Assert
        Assert.False(success);
        Assert.Contains("Insufficient", message);
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
        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var (success, _) = await _sut.BuyAsync(GuildId, UserId, item.Name);

        // Assert
        Assert.True(success);
        Assert.Equal(300, wallet.Coins); // 500 - 200
        _shopRepoMock.Verify(r => r.AddUserInventoryAsync(It.IsAny<UserInventory>()), Times.Once);
    }

    [Fact]
    public async Task Buy_FishingRodAlreadyOwned_ShouldFail()
    {
        // Arrange
        var item      = MakeItem(200, ItemType.FishingRod);
        var wallet    = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 500 };
        var existing  = new UserInventory { WalletId = wallet.Id, ItemId = item.Id, Quantity = 1 };

        _shopRepoMock.Setup(r => r.GetItemByNameAsync(GuildId, item.Name)).ReturnsAsync(item);
        _shopRepoMock.Setup(r => r.GetUserItemAsync(wallet.Id, item.Id)).ReturnsAsync(existing);
        _walletRepoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var (success, message) = await _sut.BuyAsync(GuildId, UserId, item.Name);

        // Assert
        Assert.False(success);
        Assert.Contains("already own", message);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static InventoryItem MakeItem(long price, ItemType type = ItemType.Collectible) => new()
    {
        Id = Guid.NewGuid(),
        GuildId = GuildId,
        Name = "Test Item",
        Emoji = "📦",
        Price = price,
        Type = type,
        IsAvailable = true
    };
}