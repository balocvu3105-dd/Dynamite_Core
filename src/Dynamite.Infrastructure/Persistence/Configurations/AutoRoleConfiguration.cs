// src/Dynamite.Infrastructure/Persistence/Configurations/AutoRoleConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AutoRoleConfiguration : IEntityTypeConfiguration<AutoRoleConfig>
{
    public void Configure(EntityTypeBuilder<AutoRoleConfig> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.GuildId).HasConversion<long>();
        builder.Property(a => a.RoleId).HasConversion<long>();

        // Unique: 1 guild không thể có cùng 1 roleId 2 lần trong auto roles
        builder.HasIndex(a => new { a.GuildId, a.RoleId }).IsUnique();

        builder.HasOne(a => a.GuildConfig)
            .WithMany(g => g.AutoRoles)
            .HasForeignKey(a => a.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}