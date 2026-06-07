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
        services.AddScoped<IAntiSpamRepository, AntiSpamRepository>();

        // Phase 9b
        services.AddScoped<IGuildPresenceRepository, GuildPresenceRepository>();

        return services;
    }
}