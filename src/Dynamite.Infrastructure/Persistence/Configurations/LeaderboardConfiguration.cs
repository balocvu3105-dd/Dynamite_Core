// src/Dynamite.Infrastructure/Persistence/Configurations/LeaderboardConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LeaderboardSnapshotConfiguration : IEntityTypeConfiguration<LeaderboardSnapshot>
{
    public void Configure(EntityTypeBuilder<LeaderboardSnapshot> builder)
    {
        builder.ToTable("LeaderboardSnapshots");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.GuildId, s.Type, s.WeekStartDate });
        builder.Property(s => s.Type).HasConversion<string>();

        builder.HasMany(s => s.Entries)
            .WithOne(e => e.Snapshot)
            .HasForeignKey(e => e.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LeaderboardEntryConfiguration : IEntityTypeConfiguration<LeaderboardEntry>
{
    public void Configure(EntityTypeBuilder<LeaderboardEntry> builder)
    {
        builder.ToTable("LeaderboardEntries");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.SnapshotId, e.Rank });
    }
}

public class WeeklyActivityConfiguration : IEntityTypeConfiguration<WeeklyActivity>
{
    public void Configure(EntityTypeBuilder<WeeklyActivity> builder)
    {
        builder.ToTable("WeeklyActivities");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.GuildId, a.UserId }).IsUnique();
    }
}
