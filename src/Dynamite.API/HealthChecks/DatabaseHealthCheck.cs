// src/Dynamite.API/HealthChecks/DatabaseHealthCheck.cs
namespace Dynamite.API.HealthChecks;

using Dynamite.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Ping database để verify connection còn hoạt động.
/// Dùng EF Core CanConnectAsync — không query bảng nào, chỉ kiểm tra connectivity.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public DatabaseHealthCheck(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Database connection OK")
                : HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check threw an exception", ex);
        }
    }
}