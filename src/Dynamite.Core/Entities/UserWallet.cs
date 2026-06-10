// src/Dynamite.Core/Entities/UserWallet.cs
namespace Dynamite.Core.Entities;

public class UserWallet : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long Coins { get; set; } = 0;
    public DateTime? LastDaily { get; set; }
    public int DailyStreak { get; set; } = 0;

    public ICollection<UserInventory> Inventory { get; set; } = [];
    public ICollection<Transaction> SentTransactions { get; set; } = [];
    public ICollection<Transaction> ReceivedTransactions { get; set; } = [];
}