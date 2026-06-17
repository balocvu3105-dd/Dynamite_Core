// src/Dynamite.Infrastructure/Persistence/Configurations/FishingDataSnapshotConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FishingDataSnapshotConfiguration : IEntityTypeConfiguration<FishingDataSnapshot>
{
    public void Configure(EntityTypeBuilder<FishingDataSnapshot> builder)
    {
        builder.ToTable("FishingDataSnapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Reason).HasMaxLength(64);
        builder.Property(s => s.BagSnapshotJson).HasColumnType("text");
        builder.Property(s => s.AchievementIds).HasMaxLength(2000);

        builder.HasIndex(s => new { s.GuildId, s.UserId, s.CreatedAt });
    }
}
