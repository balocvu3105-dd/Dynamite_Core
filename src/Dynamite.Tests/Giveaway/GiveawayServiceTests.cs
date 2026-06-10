// src/Dynamite.Tests/Giveaway/GiveawayServiceTests.cs
namespace Dynamite.Tests.Giveaway;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Giveaway.Services;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class GiveawayServiceTests
{
    private readonly Mock<IGiveawayRepository> _repoMock;
    private readonly Mock<DiscordSocketClient> _clientMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly GiveawayService _sut;

    private static readonly Guid GiveawayId = Guid.NewGuid();
    private const ulong GuildId   = 111111111111111111UL;
    private const ulong HostId    = 222222222222222222UL;
    private const ulong UserId    = 333333333333333333UL;
    private const ulong MessageId = 444444444444444444UL;

    public GiveawayServiceTests()
    {
        _repoMock        = new Mock<IGiveawayRepository>();
        _clientMock      = new Mock<DiscordSocketClient>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        _sut = new GiveawayService(
            _repoMock.Object,
            _clientMock.Object,
            _scopeFactoryMock.Object,
            NullLogger<GiveawayService>.Instance);
    }

    // ── Enter ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Enter_ActiveGiveaway_NewUser_ShouldSucceed()
    {
        // Arrange
        var giveaway = MakeActiveGiveaway();
        _repoMock.Setup(r => r.GetByMessageIdAsync(MessageId)).ReturnsAsync(giveaway);
        _repoMock.Setup(r => r.HasEnteredAsync(giveaway.Id, UserId)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetEntryCountAsync(giveaway.Id)).ReturnsAsync(1);

        // Act
        var (success, message) = await _sut.EnterAsync(MessageId, UserId, GuildId);

        // Assert
        Assert.True(success);
        _repoMock.Verify(r => r.AddEntryAsync(It.IsAny<GiveawayEntry>()), Times.Once);
    }

    [Fact]
    public async Task Enter_AlreadyEntered_ShouldFail()
    {
        // Arrange
        var giveaway = MakeActiveGiveaway();
        _repoMock.Setup(r => r.GetByMessageIdAsync(MessageId)).ReturnsAsync(giveaway);
        _repoMock.Setup(r => r.HasEnteredAsync(giveaway.Id, UserId)).ReturnsAsync(true);

        // Act
        var (success, message) = await _sut.EnterAsync(MessageId, UserId, GuildId);

        // Assert
        Assert.False(success);
        Assert.Contains("already entered", message);
    }

    [Fact]
    public async Task Enter_HostCannotEnterOwnGiveaway_ShouldFail()
    {
        // Arrange
        var giveaway = MakeActiveGiveaway();
        _repoMock.Setup(r => r.GetByMessageIdAsync(MessageId)).ReturnsAsync(giveaway);

        // Act — HostId trying to enter
        var (success, message) = await _sut.EnterAsync(MessageId, HostId, GuildId);

        // Assert
        Assert.False(success);
        Assert.Contains("own giveaway", message);
    }

    [Fact]
    public async Task Enter_EndedGiveaway_ShouldFail()
    {
        // Arrange
        var giveaway = MakeActiveGiveaway();
        giveaway.IsEnded = true;
        _repoMock.Setup(r => r.GetByMessageIdAsync(MessageId)).ReturnsAsync(giveaway);

        // Act
        var (success, _) = await _sut.EnterAsync(MessageId, UserId, GuildId);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task Enter_NullGiveaway_ShouldFail()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByMessageIdAsync(MessageId)).ReturnsAsync((Giveaway?)null);

        // Act
        var (success, _) = await _sut.EnterAsync(MessageId, UserId, GuildId);

        // Assert
        Assert.False(success);
    }

    // ── Reroll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reroll_NotEndedGiveaway_ShouldFail()
    {
        // Arrange
        var giveaway = MakeActiveGiveaway();
        giveaway.IsEnded = false;
        _repoMock.Setup(r => r.GetByIdAsync(giveaway.Id)).ReturnsAsync(giveaway);

        // Act
        var (success, message) = await _sut.RerollAsync(giveaway.Id, HostId);

        // Assert
        Assert.False(success);
        Assert.Contains("not ended", message);
    }

    [Fact]
    public async Task Reroll_NoEntries_ShouldFail()
    {
        // Arrange
        var giveaway = MakeActiveGiveaway();
        giveaway.IsEnded = true;
        _repoMock.Setup(r => r.GetByIdAsync(giveaway.Id)).ReturnsAsync(giveaway);
        _repoMock.Setup(r => r.GetEntriesAsync(giveaway.Id)).ReturnsAsync([]);

        // Act
        var (success, message) = await _sut.RerollAsync(giveaway.Id, HostId);

        // Assert
        Assert.False(success);
        Assert.Contains("No entries", message);
    }

    [Fact]
    public async Task Reroll_NotFound_ShouldFail()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Giveaway?)null);

        // Act
        var (success, _) = await _sut.RerollAsync(Guid.NewGuid(), HostId);

        // Assert
        Assert.False(success);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_AlreadyEnded_ShouldFail()
    {
        // Arrange
        var giveaway = MakeActiveGiveaway();
        giveaway.IsEnded = true;
        _repoMock.Setup(r => r.GetByIdAsync(giveaway.Id)).ReturnsAsync(giveaway);

        // Act
        var result = await _sut.CancelAsync(giveaway.Id);

        // Assert
        Assert.False(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Giveaway MakeActiveGiveaway() => new()
    {
        Id = GiveawayId,
        GuildId = GuildId,
        ChannelId = 555555555555555555UL,
        MessageId = MessageId,
        HostId = HostId,
        Prize = "Test Prize",
        WinnerCount = 1,
        StartsAt = DateTime.UtcNow.AddMinutes(-10),
        EndsAt = DateTime.UtcNow.AddHours(1),
        IsEnded = false,
        IsCancelled = false
    };
}