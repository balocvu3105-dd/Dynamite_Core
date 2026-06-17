// src/Dynamite.Infrastructure/Persistence/Configurations/GuildPondConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GuildPondConfiguration : IEntityTypeConfiguration<GuildPond>
{
    public void Configure(EntityTypeBuilder<GuildPond> builder)
    {
        builder.ToTable("GuildPonds");
        builder.HasKey(p => p.Id);

        // Mỗi guild có đúng 1 bể
        builder.HasIndex(p => p.GuildId).IsUnique();

        builder.Property(p => p.CurrentWeather)
            .HasConversion<string>();
    }
}
