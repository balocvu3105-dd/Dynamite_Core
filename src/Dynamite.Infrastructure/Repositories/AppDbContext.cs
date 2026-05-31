// src/Dynamite.Infrastructure/Persistence/AppDbContext.cs
namespace Dynamite.Infrastructure.Persistence;

using Dynamite.Core.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GuildConfig> GuildConfigs => Set<GuildConfig>();
    public DbSet<Warning> Warnings => Set<Warning>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();

    // Phase 3
    public DbSet<AutoRoleConfig> AutoRoleConfigs => Set<AutoRoleConfig>();
    public DbSet<RolePanel> RolePanels => Set<RolePanel>();
    public DbSet<RolePanelItem> RolePanelItems => Set<RolePanelItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}