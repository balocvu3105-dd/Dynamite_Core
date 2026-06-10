// src/Dynamite.Infrastructure/Persistence/Configurations/TicketConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.GuildId).HasConversion<long>();
        builder.Property(t => t.ChannelId).HasConversion<long>();
        builder.Property(t => t.OwnerId).HasConversion<long>();

        builder.Property(t => t.Topic).HasMaxLength(256);
        builder.Property(t => t.Status).HasConversion<string>();

        builder.HasIndex(t => t.ChannelId).IsUnique();
        builder.HasIndex(t => new { t.GuildId, t.Number }).IsUnique();
    }
}