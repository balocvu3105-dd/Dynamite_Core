// src/Dynamite.Infrastructure/Persistence/Configurations/RolePanelItemConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RolePanelItemConfiguration : IEntityTypeConfiguration<RolePanelItem>
{
    public void Configure(EntityTypeBuilder<RolePanelItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.RoleId).HasConversion<long>();

        builder.Property(i => i.Label)
            .HasMaxLength(80)   // Discord button label limit
            .IsRequired();

        builder.Property(i => i.Emoji)
            .HasMaxLength(100);

        builder.Property(i => i.Description)
            .HasMaxLength(100); // Discord select option description limit

        // 1 panel không nên có cùng roleId 2 lần
        builder.HasIndex(i => new { i.RolePanelId, i.RoleId }).IsUnique();
    }
}