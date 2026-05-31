// src/Dynamite.Infrastructure/Persistence/Configurations/RolePanelConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RolePanelConfiguration : IEntityTypeConfiguration<RolePanel>
{
    public void Configure(EntityTypeBuilder<RolePanel> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.GuildId).HasConversion<long>();
        builder.Property(p => p.ChannelId).HasConversion<long>();
        builder.Property(p => p.MessageId).HasConversion<long>();

        builder.Property(p => p.Title)
            .HasMaxLength(256)   // Discord embed title limit
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(4096); // Discord embed description limit

        builder.Property(p => p.PanelType)
            .HasConversion<int>();

        // Index để tìm panel theo message (khi xử lý interaction)
        builder.HasIndex(p => new { p.GuildId, p.MessageId });

        builder.HasOne(p => p.GuildConfig)
            .WithMany(g => g.RolePanels)
            .HasForeignKey(p => p.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Items)
            .WithOne(i => i.RolePanel)
            .HasForeignKey(i => i.RolePanelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}