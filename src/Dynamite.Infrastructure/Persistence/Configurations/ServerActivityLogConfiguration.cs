// src/Dynamite.Infrastructure/Persistence/Configurations/ServerActivityLogConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ServerActivityLogConfiguration : IEntityTypeConfiguration<ServerActivityLog>
{
    public void Configure(EntityTypeBuilder<ServerActivityLog> builder)
    {
        builder.ToTable("ServerActivityLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(l => l.EventType).HasMaxLength(64);
        builder.Property(l => l.Title).HasMaxLength(256);
        builder.Property(l => l.Description).HasMaxLength(2048);
        builder.Property(l => l.ActorId).HasMaxLength(32);
        builder.Property(l => l.ActorUsername).HasMaxLength(128);
        builder.Property(l => l.ActorAvatarUrl).HasMaxLength(512);
        builder.Property(l => l.TargetId).HasMaxLength(32);
        builder.Property(l => l.TargetUsername).HasMaxLength(128);

        // Indexes for fast querying on web dashboard
        builder.HasIndex(l => new { l.GuildId, l.CreatedAt });
        builder.HasIndex(l => new { l.GuildId, l.Category, l.CreatedAt });
        builder.HasIndex(l => l.CreatedAt); // for retention cleanup
    }
}
