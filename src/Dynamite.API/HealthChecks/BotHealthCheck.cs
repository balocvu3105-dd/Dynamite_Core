// src/Dynamite.API/HealthChecks/BotHealthCheck.cs
namespace Dynamite.API.HealthChecks;

using Dynamite.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Kiểm tra Discord bot có ready không qua IBotStatusProvider.
/// API project không cần biết về Discord.Net — chỉ đọc interface từ Shared.
/// </summary>
public sealed class BotHealthCheck : IHealthCheck
{
    private readonly IBotStatusProvider _botStatus;

    public BotHealthCheck(IBotStatusProvider botStatus)
    {
        _botStatus = botStatus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_botStatus.IsReady)
        {
            var data = new Dictionary<string, object>
            {
                ["last_ready_at"] = _botStatus.LastReadyAt?.ToString("O") ?? "unknown"
            };
            return Task.FromResult(HealthCheckResult.Healthy("Bot is connected and ready", data));
        }

        return Task.FromResult(HealthCheckResult.Degraded("Bot is not ready yet"));
    }
}