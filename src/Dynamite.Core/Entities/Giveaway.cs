// src/Dynamite.Core/Entities/Giveaway.cs
namespace Dynamite.Core.Entities;

public class Giveaway : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong HostId { get; set; }

    public string Prize { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int WinnerCount { get; set; } = 1;

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public bool IsEnded { get; set; } = false;
    public bool IsCancelled { get; set; } = false;

    // Comma-separated winner user IDs (sau khi end)
    public string? WinnerIds { get; set; }

    public ICollection<GiveawayEntry> Entries { get; set; } = [];
}