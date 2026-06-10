// src/Dynamite.Core/Entities/TicketConfig.cs
namespace Dynamite.Core.Entities;

public class TicketConfig : BaseEntity
{
    public ulong GuildId { get; set; }

    // Channel chứa panel "Open Ticket"
    public ulong PanelChannelId { get; set; }
    public ulong PanelMessageId { get; set; }

    // Role có thể xem tất cả tickets
    public ulong? StaffRoleId { get; set; }

    // Category để tạo ticket channels vào (optional)
    public ulong? CategoryId { get; set; }

    // Tự tăng mỗi lần tạo ticket mới
    public int NextTicketNumber { get; set; } = 1;

    public ICollection<Ticket> Tickets { get; set; } = [];
}