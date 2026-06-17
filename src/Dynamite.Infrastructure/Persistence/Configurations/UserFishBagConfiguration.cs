// src/Dynamite.Infrastructure/Persistence/Configurations/UserFishBagConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserFishBagConfiguration : IEntityTypeConfiguration<UserFishBag>
{
    public void Configure(EntityTypeBuilder<UserFishBag> builder)
    {
        builder.ToTable("UserFishBags");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.GuildId, b.UserId }).IsUnique();

        builder.HasMany(b => b.Fish)
            .WithOne(f => f.Bag)
            .HasForeignKey(f => f.BagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(b => b.IsFull);
        builder.Ignore(b => b.UsedSlots);
        builder.Ignore(b => b.FreeSlots);
    }
}

public class CaughtFishConfiguration : IEntityTypeConfiguration<CaughtFish>
{
    public void Configure(EntityTypeBuilder<CaughtFish> builder)
    {
        builder.ToTable("CaughtFish");
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => new { f.GuildId, f.UserId });
        builder.HasIndex(f => f.BagId);
        builder.Property(f => f.Rarity).HasMaxLength(32);
        builder.Property(f => f.SourcePool).HasMaxLength(64);
    }
}

public class GuildPearlLogConfiguration : IEntityTypeConfiguration<GuildPearlLog>
{
    public void Configure(EntityTypeBuilder<GuildPearlLog> builder)
    {
        builder.ToTable("GuildPearlLogs");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.GuildId, p.PearlType, p.CreatedAt });
        builder.Property(p => p.PearlType).HasConversion<string>();
    }
}
