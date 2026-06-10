// src/Dynamite.Infrastructure/Persistence/Configurations/TicketConfigConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TicketConfigConfiguration : IEntityTypeConfiguration<TicketConfig>
{
    public void Configure(EntityTypeBuilder<TicketConfig> builder)
    {
        builder.ToTable("TicketConfigs");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.GuildId).HasConversion<long>();
        builder.Property(t => t.PanelChannelId).HasConversion<long>();
        builder.Property(t => t.PanelMessageId).HasConversion<long>();
        builder.Property(t => t.StaffRoleId).HasConversion<long?>();
        builder.Property(t => t.CategoryId).HasConversion<long?>();

        builder.HasIndex(t => t.GuildId).IsUnique();

        builder.HasMany(t => t.Tickets)
            .WithOne(t => t.TicketConfig)
            .HasForeignKey(t => t.TicketConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}