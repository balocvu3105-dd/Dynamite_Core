// src/Dynamite.Infrastructure/Persistence/Configurations/GuildPresenceConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GuildPresenceConfiguration : IEntityTypeConfiguration<GuildPresence>
{
    public void Configure(EntityTypeBuilder<GuildPresence> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.GuildId)
            .HasConversion<long>();

        // GuildId là unique — bot chỉ có 1 entry per guild
        builder.HasIndex(g => g.GuildId).IsUnique();

        builder.Property(g => g.GuildName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.IconHash)
            .HasMaxLength(64);
    }
}