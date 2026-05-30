namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class WarningConfiguration : IEntityTypeConfiguration<Warning>
{
    public void Configure(EntityTypeBuilder<Warning> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.GuildId)
            .HasConversion<long>();

        builder.Property(w => w.TargetUserId)
            .HasConversion<long>();

        builder.Property(w => w.ModeratorId)
            .HasConversion<long>();

        builder.Property(w => w.Reason)
            .HasMaxLength(500)
            .IsRequired();
    }
}
