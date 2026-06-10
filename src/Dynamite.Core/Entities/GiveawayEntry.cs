// src/Dynamite.Core/Entities/GiveawayEntry.cs
namespace Dynamite.Core.Entities;

public class GiveawayEntry : BaseEntity
{
    public Guid GiveawayId { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public DateTime EnteredAt { get; set; }

    public Giveaway Giveaway { get; set; } = null!;
}