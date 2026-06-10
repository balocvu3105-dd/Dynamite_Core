// src/Dynamite.Tests/Economy/WalletServiceTests.cs
namespace Dynamite.Tests.Economy;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class WalletServiceTests
{
    private readonly Mock<IWalletRepository> _repoMock;
    private readonly WalletService _sut;

    private const ulong GuildId = 111111111111111111UL;
    private const ulong UserId  = 222222222222222222UL;
    private const ulong User2Id = 333333333333333333UL;

    public WalletServiceTests()
    {
        _repoMock = new Mock<IWalletRepository>();
        _sut = new WalletService(_repoMock.Object, NullLogger<WalletService>.Instance);
    }

    // ── Daily ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClaimDaily_FirstTime_ShouldGiveBaseReward()
    {
        // Arrange
        var wallet = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 0 };
        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var (success, _, coins, streak) = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.True(success);
        Assert.Equal(100, coins);
        Assert.Equal(1, streak);
        Assert.Equal(100, wallet.Coins);
    }

    [Fact]
    public async Task ClaimDaily_WithStreak_ShouldAddBonus()
    {
        // Arrange — streak 5 ngày → bonus = 4 * 10 = 40 → total = 140
        var wallet = new UserWallet
        {
            GuildId = GuildId,
            UserId = UserId,
            Coins = 0,
            LastDaily = DateTime.UtcNow.AddHours(-25),
            DailyStreak = 4
        };
        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var (success, _, coins, streak) = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.True(success);
        Assert.Equal(5, streak);
        Assert.Equal(140, coins); // 100 base + 40 bonus
    }

    [Fact]
    public async Task ClaimDaily_StreakBonus_ShouldNotExceedMax()
    {
        // Arrange — streak 30 ngày → bonus capped tại 200
        var wallet = new UserWallet
        {
            GuildId = GuildId,
            UserId = UserId,
            Coins = 0,
            LastDaily = DateTime.UtcNow.AddHours(-25),
            DailyStreak = 30
        };
        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var (success, _, coins, _) = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.True(success);
        Assert.Equal(300, coins); // 100 base + 200 max bonus
    }

    [Fact]
    public async Task ClaimDaily_ClaimedWithin24h_ShouldFail()
    {
        // Arrange
        var wallet = new UserWallet
        {
            GuildId = GuildId,
            UserId = UserId,
            LastDaily = DateTime.UtcNow.AddHours(-5) // 5 giờ trước
        };
        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var (success, message, coins, _) = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.False(success);
        Assert.Contains("already claimed", message);
        Assert.Equal(0, coins);
    }

    [Fact]
    public async Task ClaimDaily_StreakBrokenAfter48h_ShouldReset()
    {
        // Arrange — last daily 49 giờ trước → streak reset về 1
        var wallet = new UserWallet
        {
            GuildId = GuildId,
            UserId = UserId,
            LastDaily = DateTime.UtcNow.AddHours(-49),
            DailyStreak = 10
        };
        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(wallet);

        // Act
        var (success, _, _, streak) = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.True(success);
        Assert.Equal(1, streak); // reset về 1
    }

    // ── Transfer ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Transfer_ValidAmount_ShouldMoveCoins()
    {
        // Arrange
        var from = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 500 };
        var to   = new UserWallet { GuildId = GuildId, UserId = User2Id, Coins = 100 };

        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(from);
        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, User2Id)).ReturnsAsync(to);

        // Act
        var (success, _) = await _sut.TransferAsync(GuildId, UserId, User2Id, 200);

        // Assert
        Assert.True(success);
        Assert.Equal(300, from.Coins);
        Assert.Equal(300, to.Coins);
    }

    [Fact]
    public async Task Transfer_InsufficientBalance_ShouldFail()
    {
        // Arrange
        var from = new UserWallet { GuildId = GuildId, UserId = UserId, Coins = 50 };
        var to   = new UserWallet { GuildId = GuildId, UserId = User2Id, Coins = 0 };

        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, UserId)).ReturnsAsync(from);
        _repoMock.Setup(r => r.GetOrCreateAsync(GuildId, User2Id)).ReturnsAsync(to);

        // Act
        var (success, message) = await _sut.TransferAsync(GuildId, UserId, User2Id, 200);

        // Assert
        Assert.False(success);
        Assert.Contains("Insufficient", message);
        Assert.Equal(50, from.Coins); // không thay đổi
    }

    [Fact]
    public async Task Transfer_ToSelf_ShouldFail()
    {
        // Act
        var (success, message) = await _sut.TransferAsync(GuildId, UserId, UserId, 100);

        // Assert
        Assert.False(success);
        Assert.Contains("yourself", message);
    }

    [Fact]
    public async Task Transfer_ZeroAmount_ShouldFail()
    {
        // Act
        var (success, _) = await _sut.TransferAsync(GuildId, UserId, User2Id, 0);

        // Assert
        Assert.False(success);
    }
}