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

        // Fix: nullable ulong channel IDs must also use long conversion.
        // Without this, EF maps them as numeric(20,0) instead of bigint,
        // inconsistent with all other Discord ID columns.
        builder.Property(g => g.ModLogChannelId)
            .HasConversion<long?>();

        builder.Property(g => g.ServerLogChannelId)
            .HasConversion<long?>();

        builder.HasMany(g => g.Warnings)
            .WithOne(w => w.GuildConfig)
            .HasForeignKey(w => w.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.ModerationActions)
            .WithOne(a => a.GuildConfig)
            .HasForeignKey(a => a.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}