// src/Dynamite.Core/Entities/Transaction.cs
namespace Dynamite.Core.Entities;

public enum TransactionType
{
    Daily,
    Fishing,
    Transfer,
    Purchase,
    AdminGrant,
    AdminDeduct
}

public class Transaction : BaseEntity
{
    public ulong GuildId { get; set; }
    public Guid? FromWalletId { get; set; }
    public Guid? ToWalletId { get; set; }
    public long Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Note { get; set; }
    public new DateTime CreatedAt { get; set; }

    public UserWallet? FromWallet { get; set; }
    public UserWallet? ToWallet { get; set; }
}