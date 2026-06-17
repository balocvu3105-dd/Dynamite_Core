// src/Dynamite.Infrastructure/Persistence/Configurations/UserServerProfileConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserServerProfileConfiguration : IEntityTypeConfiguration<UserServerProfile>
{
    public void Configure(EntityTypeBuilder<UserServerProfile> builder)
    {
        builder.ToTable("UserServerProfiles");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.GuildId, p.UserId }).IsUnique();
    }
}
