// src/Dynamite.Tests/Economy/WalletServiceTests.cs
namespace Dynamite.Tests.Economy;

using Dynamite.Core.Common;
using Dynamite.Core.Common.Results;
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
        var result = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.True(result);
        Assert.Equal(100, result.Value!.CoinsEarned);
        Assert.Equal(1, result.Value.Streak);
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
        var result = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.True(result);
        Assert.Equal(5, result.Value!.Streak);
        Assert.Equal(140, result.Value.CoinsEarned); // 100 base + 40 bonus
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
        var result = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.True(result);
        Assert.Equal(300, result.Value!.CoinsEarned); // 100 base + 200 max bonus
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
        var result = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.False(result);
        Assert.Contains("already claimed", result.ErrorMessage);
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
        var result = await _sut.ClaimDailyAsync(GuildId, UserId);

        // Assert
        Assert.True(result);
        Assert.Equal(1, result.Value!.Streak); // reset về 1
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
        var result = await _sut.TransferAsync(GuildId, UserId, User2Id, 200);

        // Assert
        Assert.True(result);
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
        var result = await _sut.TransferAsync(GuildId, UserId, User2Id, 200);

        // Assert
        Assert.False(result);
        Assert.Contains("Insufficient", result.ErrorMessage);
        Assert.Equal(50, from.Coins); // không thay đổi
    }

    [Fact]
    public async Task Transfer_ToSelf_ShouldFail()
    {
        // Act
        var result = await _sut.TransferAsync(GuildId, UserId, UserId, 100);

        // Assert
        Assert.False(result);
        Assert.Contains("yourself", result.ErrorMessage);
    }

    [Fact]
    public async Task Transfer_ZeroAmount_ShouldFail()
    {
        // Act
        var result = await _sut.TransferAsync(GuildId, UserId, User2Id, 0);

        // Assert
        Assert.False(result);
    }
}