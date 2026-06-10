// src/Dynamite.Infrastructure/Persistence/Configurations/GiveawayEntryConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GiveawayEntryConfiguration : IEntityTypeConfiguration<GiveawayEntry>
{
    public void Configure(EntityTypeBuilder<GiveawayEntry> builder)
    {
        builder.ToTable("GiveawayEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.GuildId)
            .HasConversion<long>();

        builder.Property(e => e.UserId)
            .HasConversion<long>();

        // Prevent duplicate entries per user per giveaway
        builder.HasIndex(e => new { e.GiveawayId, e.UserId }).IsUnique();
    }
}