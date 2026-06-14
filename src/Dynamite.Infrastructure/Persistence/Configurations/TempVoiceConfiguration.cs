// src/Dynamite.Infrastructure/Persistence/Configurations/TempVoiceConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TempVoiceConfiguration : IEntityTypeConfiguration<TempVoiceConfig>
{
    public void Configure(EntityTypeBuilder<TempVoiceConfig> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.GuildId).HasConversion<long>();
        builder.Property(t => t.TriggerChannelId).HasConversion<long>();
        builder.Property(t => t.CategoryId).HasConversion<long?>();

        // 1 guild chỉ có 1 TempVoiceConfig
        builder.HasIndex(t => t.GuildId).IsUnique();

        builder.HasOne(t => t.GuildConfig)
            .WithOne(g => g.TempVoiceConfig)
            .HasForeignKey<TempVoiceConfig>(t => t.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
