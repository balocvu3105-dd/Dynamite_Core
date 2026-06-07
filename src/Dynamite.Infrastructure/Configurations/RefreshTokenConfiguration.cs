// src/Dynamite.Infrastructure/Configurations/RefreshTokenConfiguration.cs
namespace Dynamite.Infrastructure.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscordUserId)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        // Lookup nhanh khi validate refresh token
        builder.HasIndex(x => x.Token).IsUnique();

        // Cleanup token theo user
        builder.HasIndex(x => x.DiscordUserId);

        // Ignore computed properties — không map vào DB
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsActive);
    }
}