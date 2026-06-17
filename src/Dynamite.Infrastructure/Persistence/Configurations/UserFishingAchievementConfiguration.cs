// src/Dynamite.Infrastructure/Persistence/Configurations/UserFishingAchievementConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserFishingAchievementConfiguration : IEntityTypeConfiguration<UserFishingAchievement>
{
    public void Configure(EntityTypeBuilder<UserFishingAchievement> builder)
    {
        builder.ToTable("UserFishingAchievements");
        builder.HasKey(a => a.Id);

        // Mỗi achievement chỉ được award 1 lần per user per guild
        builder.HasIndex(a => new { a.GuildId, a.UserId, a.AchievementId }).IsUnique();

        builder.Property(a => a.AchievementId).HasMaxLength(64);

        // Navigation về UserFishingProfile qua composite key
        builder.HasOne(a => a.Profile)
            .WithMany(p => p.Achievements)
            .HasForeignKey(a => new { a.GuildId, a.UserId })
            .HasPrincipalKey(p => new { p.GuildId, p.UserId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
