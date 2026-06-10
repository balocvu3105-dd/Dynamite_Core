// src/Dynamite.Infrastructure/Persistence/Configurations/UserInventoryConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserInventoryConfiguration : IEntityTypeConfiguration<UserInventory>
{
    public void Configure(EntityTypeBuilder<UserInventory> builder)
    {
        builder.ToTable("UserInventories");
        builder.HasKey(u => u.Id);

        builder.HasIndex(u => new { u.WalletId, u.ItemId }).IsUnique();
    }
}