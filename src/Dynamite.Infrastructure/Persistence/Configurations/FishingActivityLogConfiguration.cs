// src/Dynamite.Infrastructure/Persistence/Configurations/FishingActivityLogConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FishingActivityLogConfiguration : IEntityTypeConfiguration<FishingActivityLog>
{
    public void Configure(EntityTypeBuilder<FishingActivityLog> builder)
    {
        builder.ToTable("FishingActivityLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Event).HasConversion<string>().HasMaxLength(32);
        builder.Property(l => l.FishName).HasMaxLength(100);
        builder.Property(l => l.Rarity).HasMaxLength(32);
        builder.Property(l => l.PoolName).HasMaxLength(100);
        builder.Property(l => l.RodName).HasMaxLength(100);
        builder.Property(l => l.Weather).HasMaxLength(32);

        // Indexes for fast querying
        builder.HasIndex(l => new { l.GuildId, l.UserId, l.CreatedAt });
        builder.HasIndex(l => new { l.GuildId, l.CreatedAt });
        builder.HasIndex(l => l.CreatedAt); // for cleanup job
    }
}
