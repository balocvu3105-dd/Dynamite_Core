// src/Dynamite.Infrastructure/Persistence/Configurations/InventoryItemConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.GuildId).HasConversion<long>();
        builder.Property(i => i.Name).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(256);
        builder.Property(i => i.Emoji).HasMaxLength(8);
        builder.Property(i => i.Type).HasConversion<string>();

        builder.HasIndex(i => new { i.GuildId, i.Name }).IsUnique();

        builder.HasMany(i => i.Owners)
            .WithOne(u => u.Item)
            .HasForeignKey(u => u.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}