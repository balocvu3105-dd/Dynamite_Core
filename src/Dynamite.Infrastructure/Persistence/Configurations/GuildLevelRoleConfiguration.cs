// src/Dynamite.Infrastructure/Persistence/Configurations/GuildLevelRoleConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GuildLevelRoleConfiguration : IEntityTypeConfiguration<GuildLevelRole>
{
    public void Configure(EntityTypeBuilder<GuildLevelRole> builder)
    {
        builder.ToTable("GuildLevelRoles");
        builder.HasKey(r => r.Id);

        // Mỗi (guild, type, level) chỉ map 1 role
        builder.HasIndex(r => new { r.GuildId, r.LevelType, r.RequiredLevel }).IsUnique();

        builder.Property(r => r.LevelType).HasConversion<string>();
    }
}
