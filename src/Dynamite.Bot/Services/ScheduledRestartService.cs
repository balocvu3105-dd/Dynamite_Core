// src/Dynamite.Bot/Services/ScheduledRestartService.cs
namespace Dynamite.Bot.Services;

using Dynamite.Bot.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// BackgroundService chạy song song với bot.
/// Tính thời điểm restart tiếp theo dựa theo config, delay đến lúc đó,
/// rồi gọi IHostApplicationLifetime.StopApplication().
///
/// Docker Compose với restart: unless-stopped sẽ tự khởi động lại process.
/// Không cần cron job ngoài.
/// </summary>
public sealed class ScheduledRestartService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ScheduledRestartSettings _settings;
    private readonly ILogger<ScheduledRestartService> _logger;

    public ScheduledRestartService(
        IHostApplicationLifetime lifetime,
        IOptions<ScheduledRestartSettings> settings,
        ILogger<ScheduledRestartService> logger)
    {
        _lifetime = lifetime;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("ScheduledRestartService is disabled via config.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRestart();

            _logger.LogInformation(
                "Next scheduled restart in {Hours}h {Minutes}m (at {Time} {TZ})",
                (int)delay.TotalHours,
                delay.Minutes,
                $"{_settings.Hour:D2}:{_settings.Minute:D2}",
                _settings.TimeZoneId);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host đang shutdown — không phải lỗi, thoát bình thường
                return;
            }

            _logger.LogWarning(
                "Scheduled restart triggered at {Time} {TZ}. Stopping host...",
                DateTime.UtcNow,
                _settings.TimeZoneId);

            _lifetime.StopApplication();

            // BUG FIX: Task.Delay(60s, stoppingToken) trước đây bị cancel ngay
            // lập tức vì StopApplication() sẽ signal stoppingToken cancel.
            // Kết quả: delay không thực sự chờ — OperationCanceledException bị throw,
            // while loop thoát mà không chờ được 60 giây như comment gợi ý.
            //
            // Fix: return ngay sau StopApplication(). while loop sẽ không chạy
            // lại vì stoppingToken.IsCancellationRequested sẽ là true.
            return;
        }
    }

    private TimeSpan CalculateDelayUntilNextRestart()
    {
        var tz = ResolveTimeZone();
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        var todayRestart = new DateTime(
            nowLocal.Year, nowLocal.Month, nowLocal.Day,
            _settings.Hour, _settings.Minute, 0,
            DateTimeKind.Unspecified);

        // Nếu giờ restart hôm nay đã qua rồi → lên lịch cho ngày mai
        var nextRestart = nowLocal < todayRestart
            ? todayRestart
            : todayRestart.AddDays(1);

        var nextRestartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(nextRestart, DateTimeKind.Unspecified), tz);

        return nextRestartUtc - DateTime.UtcNow;
    }

    /// <summary>
    /// Thử parse TimeZoneId theo cả Windows lẫn IANA format.
    /// Windows: "SE Asia Standard Time"
    /// Linux:   "Asia/Ho_Chi_Minh"
    /// </summary>
    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning(
                "TimeZone '{Id}' not found. Falling back to UTC.", _settings.TimeZoneId);
            return TimeZoneInfo.Utc;
        }
    }
}