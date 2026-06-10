// src/Dynamite.Modules.Giveaway/Services/GiveawayTimerService.cs
namespace Dynamite.Modules.Giveaway.Services;

using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class GiveawayTimerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GiveawayTimerService> _logger;
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public GiveawayTimerService(
        IServiceScopeFactory scopeFactory,
        ILogger<GiveawayTimerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GiveawayTimerService started, polling every {Interval}s",
            _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);

            try
            {
                await ProcessExpiredGiveawaysAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired giveaways");
            }
        }
    }

    private async Task ProcessExpiredGiveawaysAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGiveawayRepository>();
        var service = scope.ServiceProvider.GetRequiredService<GiveawayService>();

        var expired = await repo.GetExpiredUnendedAsync();
        if (expired.Count == 0) return;

        _logger.LogInformation("Processing {Count} expired giveaway(s)", expired.Count);

        foreach (var giveaway in expired)
        {
            await service.EndGiveawayAsync(giveaway);
        }
    }
}