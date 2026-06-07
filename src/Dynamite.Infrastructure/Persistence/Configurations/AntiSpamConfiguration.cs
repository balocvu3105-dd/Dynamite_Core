// src/Dynamite.Infrastructure/Persistence/Configurations/AntiSpamConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AntiSpamConfiguration : IEntityTypeConfiguration<AntiSpamConfig>
{
    public void Configure(EntityTypeBuilder<AntiSpamConfig> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.GuildId).HasConversion<long>();

        // 1 guild chỉ có 1 AntiSpamConfig
        builder.HasIndex(a => a.GuildId).IsUnique();

        builder.HasOne(a => a.GuildConfig)
            .WithOne(g => g.AntiSpamConfig)
            .HasForeignKey<AntiSpamConfig>(a => a.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}