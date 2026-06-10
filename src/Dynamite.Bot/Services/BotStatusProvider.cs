// src/Dynamite.Bot/Services/BotStatusProvider.cs
namespace Dynamite.Bot.Services;

using Dynamite.Shared;

/// <summary>
/// Singleton, thread-safe implementation của IBotStatusProvider.
/// BotHostedService set trạng thái; API health check đọc trạng thái.
/// </summary>
public sealed class BotStatusProvider : IBotStatusProvider
{
    private volatile bool _isReady;
    private DateTime? _lastReadyAt;

    public bool IsReady => _isReady;
    public DateTime? LastReadyAt => _lastReadyAt;

    public void SetReady()
    {
        _isReady = true;
        _lastReadyAt = DateTime.UtcNow;
    }

    public void SetNotReady()
    {
        _isReady = false;
    }
}