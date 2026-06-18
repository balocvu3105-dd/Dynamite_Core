// src/Dynamite.Tests/Ticket/TicketServiceTests.cs
namespace Dynamite.Tests.Ticket;

using Dynamite.Core.Common;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Ticket.Services;
using Discord.WebSocket;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _repoMock;
    private readonly Mock<DiscordSocketClient> _clientMock;
    private readonly TicketService _sut;

    private const ulong GuildId  = 111111111111111111UL;
    private const ulong UserId   = 222222222222222222UL;
    private const ulong ChannelId = 333333333333333333UL;

    public TicketServiceTests()
    {
        _repoMock   = new Mock<ITicketRepository>();
        _clientMock = new Mock<DiscordSocketClient>();
        _sut = new TicketService(
            _repoMock.Object,
            _clientMock.Object,
            NullLogger<TicketService>.Instance);
    }

    // ── Open ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task OpenTicket_NoConfig_ShouldFail()
    {
        // Arrange
        _repoMock.Setup(r => r.GetConfigAsync(GuildId)).ReturnsAsync((TicketConfig?)null);

        // Act
        var result = await _sut.OpenTicketAsync(GuildId, UserId);

        // Assert
        Assert.False(result);
        Assert.Contains("not set up", result.ErrorMessage);
    }

    [Fact]
    public async Task OpenTicket_UserAlreadyHasOpenTicket_ShouldFail()
    {
        // Arrange
        var config = MakeConfig();
        var existingTicket = new Core.Entities.Ticket
        {
            GuildId = GuildId,
            OwnerId = UserId,
            ChannelId = ChannelId,
            Status = TicketStatus.Open
        };

        _repoMock.Setup(r => r.GetConfigAsync(GuildId)).ReturnsAsync(config);
        _repoMock.Setup(r => r.GetOpenTicketByOwnerAsync(GuildId, UserId)).ReturnsAsync(existingTicket);

        // Act
        var result = await _sut.OpenTicketAsync(GuildId, UserId);

        // Assert
        Assert.False(result);
        Assert.Contains("already have", result.ErrorMessage);
    }

    // ── Close ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseTicket_NotFound_ShouldFail()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByChannelIdAsync(ChannelId)).ReturnsAsync((Core.Entities.Ticket?)null);

        // Act
        var result = await _sut.CloseTicketAsync(ChannelId, UserId);

        // Assert
        Assert.False(result);
        Assert.Contains("not an open ticket", result.ErrorMessage);
    }

    [Fact]
    public async Task CloseTicket_AlreadyClosed_ShouldFail()
    {
        // Arrange
        var ticket = MakeTicket(TicketStatus.Closed);
        _repoMock.Setup(r => r.GetByChannelIdAsync(ChannelId)).ReturnsAsync(ticket);

        // Act
        var result = await _sut.CloseTicketAsync(ChannelId, UserId);

        // Assert
        Assert.False(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTicket_AlreadyDeleted_ShouldFail()
    {
        // Arrange
        var ticket = MakeTicket(TicketStatus.Deleted);
        _repoMock.Setup(r => r.GetByChannelIdAsync(ChannelId)).ReturnsAsync(ticket);

        // Act
        var result = await _sut.DeleteTicketAsync(ChannelId, UserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteTicket_NotFound_ShouldFail()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByChannelIdAsync(ChannelId)).ReturnsAsync((Core.Entities.Ticket?)null);

        // Act
        var result = await _sut.DeleteTicketAsync(ChannelId, UserId);

        // Assert
        Assert.False(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TicketConfig MakeConfig() => new()
    {
        Id = Guid.NewGuid(),
        GuildId = GuildId,
        PanelChannelId = 444444444444444444UL,
        PanelMessageId = 555555555555555555UL,
        NextTicketNumber = 1
    };

    private static Core.Entities.Ticket MakeTicket(TicketStatus status) => new()
    {
        Id = Guid.NewGuid(),
        GuildId = GuildId,
        ChannelId = ChannelId,
        OwnerId = UserId,
        Number = 1,
        Status = status,
        OpenedAt = DateTime.UtcNow.AddMinutes(-30)
    };
}