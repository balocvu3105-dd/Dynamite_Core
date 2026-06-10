// src/Dynamite.Core/Entities/Ticket.cs
namespace Dynamite.Core.Entities;

public enum TicketStatus
{
    Open,
    Closed,
    Deleted
}

public class Ticket : BaseEntity
{
    public Guid TicketConfigId { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong OwnerId { get; set; }
    public int Number { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public string? Topic { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public TicketConfig TicketConfig { get; set; } = null!;
}