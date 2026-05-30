namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GuildConfigConfiguration : IEntityTypeConfiguration<GuildConfig>
{
    public void Configure(EntityTypeBuilder<GuildConfig> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.GuildId)
            .HasConversion<long>();

        builder.HasIndex(g => g.GuildId).IsUnique();

        builder.Property(g => g.GuildName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasMany(g => g.Warnings)
            .WithOne(w => w.GuildConfig)
            .HasForeignKey(w => w.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
