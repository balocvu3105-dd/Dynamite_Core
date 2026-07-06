// src/Dynamite.Infrastructure/Persistence/Configurations/UserWalletConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserWalletConfiguration : IEntityTypeConfiguration<UserWallet>
{
    public void Configure(EntityTypeBuilder<UserWallet> builder)
    {
        builder.ToTable("UserWallets");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.GuildId).HasConversion<long>();
        builder.Property(w => w.UserId).HasConversion<long>();
        builder.Property(w => w.Coins).IsConcurrencyToken();

        builder.HasIndex(w => new { w.GuildId, w.UserId }).IsUnique();
        builder.HasIndex(w => new { w.GuildId, w.Coins });

        builder.HasMany(w => w.Inventory)
            .WithOne(i => i.Wallet)
            .HasForeignKey(i => i.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.SentTransactions)
            .WithOne(t => t.FromWallet)
            .HasForeignKey(t => t.FromWalletId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(w => w.ReceivedTransactions)
            .WithOne(t => t.ToWallet)
            .HasForeignKey(t => t.ToWalletId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}