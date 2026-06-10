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
    public DbSet<AutoRoleConfig> AutoRoleConfigs => Set<AutoRoleConfig>();
    public DbSet<RolePanel> RolePanels => Set<RolePanel>();
    public DbSet<RolePanelItem> RolePanelItems => Set<RolePanelItem>();
    public DbSet<AntiSpamConfig> AntiSpamConfigs => Set<AntiSpamConfig>();

    // Phase 9a
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Phase 9b
    public DbSet<GuildPresence> GuildPresences => Set<GuildPresence>();

    // Phase 10a
    public DbSet<Giveaway> Giveaways => Set<Giveaway>();
    public DbSet<GiveawayEntry> GiveawayEntries => Set<GiveawayEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}