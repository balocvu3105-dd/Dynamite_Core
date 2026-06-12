// src/Dynamite.Infrastructure/Persistence/Configurations/GuildConfigConfiguration.cs
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

        builder.Property(g => g.ModLogChannelId)
            .HasConversion<long?>();

        // Logging channels (Phase 6)
        builder.Property(g => g.ServerLogChannelId)
            .HasConversion<long?>();

        builder.Property(g => g.MessageLogChannelId)
            .HasConversion<long?>();

        builder.Property(g => g.MemberLogChannelId)
            .HasConversion<long?>();

        builder.Property(g => g.VoiceLogChannelId)
            .HasConversion<long?>();

        // Welcome + Verify (Phase 7)
        builder.Property(g => g.WelcomeChannelId)
            .HasConversion<long?>();

        builder.Property(g => g.VerifyChannelId)
            .HasConversion<long?>();

        builder.Property(g => g.VerifyRoleId)
            .HasConversion<long?>();

        builder.Property(g => g.VerifyRemoveRoleId)
            .HasConversion<long?>();

        builder.Property(g => g.WelcomeMessage)
            .HasMaxLength(500);

        builder.Property(g => g.WelcomeEmbedTitle)
            .HasMaxLength(256);

        builder.Property(g => g.WelcomeEmbedColor)
            .HasMaxLength(16);

        builder.Property(g => g.WelcomeEmbedFooter)
            .HasMaxLength(256);

        // Default TRUE ở DB level — nếu không, migration sẽ set false
        // cho mọi guild hiện có → ảnh welcome đột nhiên biến mất
        builder.Property(g => g.WelcomeImageEnabled)
            .HasDefaultValue(true);

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