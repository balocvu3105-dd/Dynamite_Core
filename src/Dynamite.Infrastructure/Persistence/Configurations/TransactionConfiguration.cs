// src/Dynamite.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs
namespace Dynamite.Infrastructure.Persistence.Configurations;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.GuildId).HasConversion<long>();
        builder.Property(t => t.Type).HasConversion<string>();
        builder.Property(t => t.Note).HasMaxLength(256);

        builder.HasIndex(t => new { t.GuildId, t.CreatedAt });
    }
}