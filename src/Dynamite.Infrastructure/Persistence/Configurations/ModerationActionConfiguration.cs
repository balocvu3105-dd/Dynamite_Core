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

        builder.Property(a => a.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.ActionType)
            .HasConversion<int>();

        builder.HasIndex(a => new { a.GuildId, a.TargetUserId });
    }
}
