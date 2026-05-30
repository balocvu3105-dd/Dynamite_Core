namespace Dynamite.Infrastructure.Persistence;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GuildConfig> GuildConfigs => Set<GuildConfig>();
    public DbSet<Warning> Warnings => Set<Warning>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
