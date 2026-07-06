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
    public DbSet<UserBlacklist> UserBlacklists => Set<UserBlacklist>();
    public DbSet<AutoRoleConfig> AutoRoleConfigs => Set<AutoRoleConfig>();
    public DbSet<RolePanel> RolePanels => Set<RolePanel>();
    public DbSet<RolePanelItem> RolePanelItems => Set<RolePanelItem>();
    public DbSet<AntiSpamConfig> AntiSpamConfigs => Set<AntiSpamConfig>();

    // Phase 9a
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Phase 9b
    public DbSet<GuildPresence> GuildPresences => Set<GuildPresence>();
    public DbSet<ServerActivityLog> ServerActivityLogs => Set<ServerActivityLog>();

    // Phase 10a
    public DbSet<Giveaway> Giveaways => Set<Giveaway>();
    public DbSet<GiveawayEntry> GiveawayEntries => Set<GiveawayEntry>();

    // Phase 10b
    public DbSet<TicketConfig> TicketConfigs => Set<TicketConfig>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    // Phase 10c
    public DbSet<UserWallet> UserWallets => Set<UserWallet>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<UserInventory> UserInventories => Set<UserInventory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    // Phase 5 — Temp Voice
    public DbSet<TempVoiceConfig> TempVoiceConfigs => Set<TempVoiceConfig>();

    // Economy v2
    public DbSet<GuildPond> GuildPonds => Set<GuildPond>();
    public DbSet<UserFishingProfile> UserFishingProfiles => Set<UserFishingProfile>();
    public DbSet<UserServerProfile> UserServerProfiles => Set<UserServerProfile>();
    public DbSet<UserFishingAchievement> UserFishingAchievements => Set<UserFishingAchievement>();
    public DbSet<GuildLevelRole> GuildLevelRoles => Set<GuildLevelRole>();

    // Economy v2.2 — Trophies (Collector leaderboard)
    public DbSet<UserFishTrophy> UserFishTrophies => Set<UserFishTrophy>();

    // Economy v2.1 — Fish Bag, Special Pools, Leaderboards, Activity Logs
    public DbSet<FishingActivityLog> FishingActivityLogs => Set<FishingActivityLog>();
    public DbSet<FishingDataSnapshot> FishingDataSnapshots => Set<FishingDataSnapshot>();
    public DbSet<UserFishBag> UserFishBags => Set<UserFishBag>();
    public DbSet<CaughtFish> CaughtFish => Set<CaughtFish>();
    public DbSet<GuildPearlLog> GuildPearlLogs => Set<GuildPearlLog>();
    public DbSet<SpecialPool> SpecialPools => Set<SpecialPool>();
    public DbSet<LeaderboardSnapshot> LeaderboardSnapshots => Set<LeaderboardSnapshot>();
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
    public DbSet<WeeklyActivity> WeeklyActivities => Set<WeeklyActivity>();
    public DbSet<FishEncyclopediaEntry> FishEncyclopedia => Set<FishEncyclopediaEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}