// src/Dynamite.Infrastructure/DependencyInjection.cs
namespace Dynamite.Infrastructure;

using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Dynamite.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IGuildConfigRepository, GuildConfigRepository>();
        services.AddScoped<IWarningRepository, WarningRepository>();
        services.AddScoped<IModerationRepository, ModerationRepository>();
        services.AddScoped<IBlacklistRepository, BlacklistRepository>();
        services.AddScoped<IAntiSpamRepository, AntiSpamRepository>();
        services.AddScoped<IAutoRoleRepository, AutoRoleRepository>();
        services.AddScoped<IRolePanelRepository, RolePanelRepository>();
        services.AddScoped<IGiveawayRepository, GiveawayRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IGuildPresenceRepository, GuildPresenceRepository>();
        services.AddScoped<ITempVoiceRepository, TempVoiceRepository>();

        // Economy repositories
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IPondRepository, PondRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IFishBagRepository, FishBagRepository>();
        services.AddScoped<ISpecialPoolRepository, SpecialPoolRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
        services.AddScoped<IFishingLogRepository, FishingLogRepository>();
        services.AddScoped<IFishingSnapshotRepository, FishingSnapshotRepository>();
        services.AddScoped<IFishTrophyRepository, FishTrophyRepository>();
        services.AddScoped<IFishEncyclopediaRepository, FishEncyclopediaRepository>();

        return services;
    }
}