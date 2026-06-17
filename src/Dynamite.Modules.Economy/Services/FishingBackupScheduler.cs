// src/Dynamite.Modules.Economy/Services/FishingBackupScheduler.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Chạy nền — tạo weekly snapshot và prune activity logs cũ.
///
/// Lịch:
/// - Chủ Nhật 12:05 UTC → backup toàn bộ user có hoạt động trong tuần
///   (5 phút sau LeaderboardHostedService để tránh tranh chấp DB)
/// - Mỗi ngày 03:00 UTC → prune FishingActivityLog cũ hơn 90 ngày
///
/// Tại sao KHÔNG backup mỗi giờ:
///   - 1000 users × 1 snapshot/giờ = 24,000 rows/ngày + JSON blobs
///   - Dùng milestone trigger (level up, mythic, pearl) + weekly là đủ
///   - Admin luôn có thể manual backup khi cần
/// </summary>
public class FishingBackupScheduler : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FishingBackupScheduler> _logger;

    // Track ngày đã chạy backup và prune
    private DateTime? _lastBackupDate;
    private DateTime? _lastPruneDate;

    public FishingBackupScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<FishingBackupScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FishingBackupScheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            try
            {
                // Weekly backup: Chủ Nhật 12:05–12:06 UTC
                if (now.DayOfWeek == DayOfWeek.Sunday
                    && now.Hour == 12 && now.Minute == 5
                    && _lastBackupDate?.Date != now.Date)
                {
                    _lastBackupDate = now;
                    await RunWeeklyBackupAsync(stoppingToken);
                }

                // Daily prune: 03:00–03:01 UTC
                if (now.Hour == 3 && now.Minute == 0
                    && _lastPruneDate?.Date != now.Date)
                {
                    _lastPruneDate = now;
                    await RunDailyPruneAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FishingBackupScheduler error");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunWeeklyBackupAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting weekly fishing backup...");

        using var scope      = _scopeFactory.CreateScope();
        var pondRepo         = scope.ServiceProvider.GetRequiredService<IPondRepository>();
        var profileRepo      = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
        var snapshotService  = scope.ServiceProvider.GetRequiredService<FishingSnapshotService>();

        var allPonds = await pondRepo.GetAllAsync();
        var count    = 0;

        foreach (var pond in allPonds)
        {
            try
            {
                // Chỉ backup user đã câu ít nhất 1 lần (TotalCaught > 0)
                var activeUserIds = await profileRepo.GetActiveUserIdsAsync(pond.GuildId);

                foreach (var userId in activeUserIds)
                {
                    await snapshotService.CreateSnapshotAsync(pond.GuildId, userId, "auto-weekly");
                    count++;
                    // Spread DB writes — tránh spike khi có nhiều user
                    await Task.Delay(100, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weekly backup failed for guild {GuildId}", pond.GuildId);
            }
        }

        _logger.LogInformation("Weekly fishing backup completed — {Count} users processed", count);
    }

    private async Task RunDailyPruneAsync()
    {
        _logger.LogInformation("Pruning old fishing activity logs (>90 days)...");

        using var scope  = _scopeFactory.CreateScope();
        var fishLogRepo  = scope.ServiceProvider.GetRequiredService<IFishingLogRepository>();

        await fishLogRepo.PruneOldLogsAsync(olderThanDays: 90);
        await fishLogRepo.SaveChangesAsync();

        _logger.LogInformation("Fishing log prune completed");
    }
}
