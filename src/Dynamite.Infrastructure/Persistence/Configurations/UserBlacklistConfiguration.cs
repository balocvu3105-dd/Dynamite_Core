// src/Dynamite.Infrastructure/Persistence/Configurations/UserBlacklistConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserBlacklistConfiguration : IEntityTypeConfiguration<UserBlacklist>
{
    public void Configure(EntityTypeBuilder<UserBlacklist> builder)
    {
        builder.HasKey(b => b.Id);

        // ulong → long: PostgreSQL has no unsigned BIGINT, so we store as signed long.
        builder.Property(b => b.GuildId).HasConversion<long>();
        builder.Property(b => b.TargetUserId).HasConversion<long>();
        builder.Property(b => b.ModeratorId).HasConversion<long>();
        builder.Property(b => b.RemovedByModeratorId).HasConversion<long?>();

        builder.Property(b => b.TargetUsername).HasMaxLength(100).IsRequired();
        builder.Property(b => b.TargetAvatarUrl).HasMaxLength(512);
        builder.Property(b => b.Reason).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(2000);
        builder.Property(b => b.RemoveReason).HasMaxLength(500);

        // Primary lookup: active entry for a specific user in a specific guild.
        builder.HasIndex(b => new { b.GuildId, b.TargetUserId });

        // Fast "is user active-blacklisted?" check used by the event handler.
        builder.HasIndex(b => new { b.GuildId, b.TargetUserId, b.IsActive });

        builder.HasOne(b => b.GuildConfig)
            .WithMany(g => g.Blacklist)
            .HasForeignKey(b => b.GuildConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
