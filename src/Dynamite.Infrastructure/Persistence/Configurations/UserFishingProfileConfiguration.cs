// src/Dynamite.Infrastructure/Persistence/Configurations/UserFishingProfileConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserFishingProfileConfiguration : IEntityTypeConfiguration<UserFishingProfile>
{
    public void Configure(EntityTypeBuilder<UserFishingProfile> builder)
    {
        builder.ToTable("UserFishingProfiles");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.GuildId, p.UserId }).IsUnique();

        builder.HasMany(p => p.Achievements)
            .WithOne(a => a.Profile)
            .HasForeignKey(a => new { a.GuildId, a.UserId })
            .HasPrincipalKey(p => new { p.GuildId, p.UserId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
