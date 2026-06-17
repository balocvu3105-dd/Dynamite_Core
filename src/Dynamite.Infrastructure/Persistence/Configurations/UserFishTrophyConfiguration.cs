// src/Dynamite.Infrastructure/Persistence/Configurations/UserFishTrophyConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserFishTrophyConfiguration : IEntityTypeConfiguration<UserFishTrophy>
{
    public void Configure(EntityTypeBuilder<UserFishTrophy> builder)
    {
        builder.ToTable("UserFishTrophies");

        builder.HasKey(t => t.Id);

        // Unique: 1 user chỉ có 1 trophy per fish name per guild
        builder.HasIndex(t => new { t.GuildId, t.UserId, t.FishName })
            .IsUnique();

        // Index cho leaderboard query (GROUP BY UserId WHERE GuildId)
        builder.HasIndex(t => new { t.GuildId, t.UserId });

        builder.Property(t => t.FishName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Rarity).HasMaxLength(20).IsRequired();
    }
}
