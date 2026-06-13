// src/Dynamite.Infrastructure/Persistence/Configurations/GiveawayConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GiveawayConfiguration : IEntityTypeConfiguration<Giveaway>
{
    public void Configure(EntityTypeBuilder<Giveaway> builder)
    {
        builder.ToTable("Giveaways");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.GuildId)
            .HasConversion<long>();

        builder.Property(g => g.ChannelId)
            .HasConversion<long>();

        builder.Property(g => g.MessageId)
            .HasConversion<long>();

        builder.Property(g => g.HostId)
            .HasConversion<long>();

        builder.Property(g => g.Prize)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(g => g.Description)
            .HasMaxLength(1024);

        builder.Property(g => g.WinnerIds)
            .HasMaxLength(512);

        builder.Property(g => g.PreSelectedWinnerIds)
            .HasMaxLength(512);

        builder.Property(g => g.PingRoleId)
            .HasConversion<long?>();

        builder.Property(g => g.ClaimMessage)
            .HasMaxLength(1024);

        builder.HasMany(g => g.Entries)
            .WithOne(e => e.Giveaway)
            .HasForeignKey(e => e.GiveawayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => new { g.GuildId, g.IsEnded });
        builder.HasIndex(g => g.MessageId).IsUnique();
    }
}