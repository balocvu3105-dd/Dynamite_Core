// src/Dynamite.Application/DependencyInjection.cs
namespace Dynamite.Application;

using Dynamite.Application.Interfaces;
using Dynamite.Application.Services;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGuildConfigService, GuildConfigService>();
        services.AddScoped<IModerationService, ModerationService>();
        services.AddScoped<IServerLogService, ServerLogService>();
        services.AddScoped<IWelcomeService, WelcomeService>();
        services.AddScoped<IAntiSpamService, AntiSpamService>();
        services.AddScoped<IBlacklistService, BlacklistService>();
        return services;
    }
}
