namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.GuildId).HasConversion<long>();
        builder.Property(a => a.TargetUserId).HasConversion<long>();
        builder.Property(a => a.ModeratorId).HasConversion<long>();
        builder.Property(a => a.TargetUsername).HasMaxLength(100);
        builder.Property(a => a.ModeratorUsername).HasMaxLength(100);

        builder.Property(a => a.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.ActionType)
            .HasConversion<int>();

        builder.HasIndex(a => new { a.GuildId, a.TargetUserId });

        // Fix: explicitly declare the relationship instead of relying on EF convention.
        // This prevents silent behavior changes if entity or FK property naming ever drifts.
        builder.HasOne(a => a.GuildConfig)
            .WithMany(g => g.ModerationActions)
            .HasForeignKey(a => a.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}