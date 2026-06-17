// src/Dynamite.Infrastructure/Persistence/Configurations/SpecialPoolConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SpecialPoolConfiguration : IEntityTypeConfiguration<SpecialPool>
{
    public void Configure(EntityTypeBuilder<SpecialPool> builder)
    {
        builder.ToTable("SpecialPools");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.GuildId, p.StartsAt, p.ExpiresAt });
        builder.Property(p => p.DropTable).HasConversion<string>();
        builder.Property(p => p.PoolName).HasMaxLength(64);

        builder.Ignore(p => p.IsActive);
        builder.Ignore(p => p.IsExpired);
    }
}
