// src/Dynamite.Modules.Economy/Services/WeatherService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Quản lý thời tiết bể cá: xoay vòng mỗi 2 tiếng, ảnh hưởng drop rate.
/// Sunny → Rainy → Stormy → Sunny...
///
/// Optimization: cache thời tiết per-guild trong IMemoryCache (singleton).
/// → Eliminates 1 DB read per cast khi weather còn hiệu lực (99% trường hợp).
/// Cache TTL = thời gian còn lại đến lần advance kế tiếp.
/// </summary>
public class WeatherService
{
    private static readonly PondWeather[] _cycle =
        [PondWeather.Sunny, PondWeather.Rainy, PondWeather.Stormy];

    private static readonly TimeSpan _duration = TimeSpan.FromHours(2);

    private readonly IPondRepository _pondRepo;
    private readonly IMemoryCache    _cache;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        IPondRepository pondRepo,
        IMemoryCache cache,
        ILogger<WeatherService> logger)
    {
        _pondRepo = pondRepo;
        _cache    = cache;
        _logger   = logger;
    }

    /// <summary>
    /// Trả về thời tiết hiện tại.
    /// Cache hit → 0 DB reads. Cache miss / hết hạn → 1 DB read + advance nếu cần.
    /// </summary>
    public async Task<PondWeather> GetCurrentWeatherAsync(ulong guildId)
    {
        var cacheKey = $"weather:{guildId}";

        if (_cache.TryGetValue(cacheKey, out PondWeather cached))
            return cached;

        // Cache miss — load từ DB
        var pond = await _pondRepo.GetOrCreateAsync(guildId);
        var now  = DateTime.UtcNow;

        if (now >= pond.WeatherExpiresAt)
        {
            // Advance thời tiết
            var idx = Array.IndexOf(_cycle, pond.CurrentWeather);
            pond.CurrentWeather   = _cycle[(idx + 1) % _cycle.Length];
            pond.WeatherExpiresAt = now.Add(_duration);
            await _pondRepo.SaveChangesAsync();

            _logger.LogInformation("[Guild {GuildId}] Weather → {Weather} (expires {Exp:HH:mm} UTC)",
                guildId, pond.CurrentWeather, pond.WeatherExpiresAt);
        }

        // Cache cho đến khi weather expire
        var ttl = pond.WeatherExpiresAt - now;
        _cache.Set(cacheKey, pond.CurrentWeather,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });

        return pond.CurrentWeather;
    }

    /// <summary>Force weather — cũng update cache.</summary>
    public async Task ForceWeatherAsync(ulong guildId, PondWeather weather, int minutes)
    {
        var pond = await _pondRepo.GetOrCreateAsync(guildId);
        pond.CurrentWeather   = weather;
        pond.WeatherExpiresAt = DateTime.UtcNow.AddMinutes(minutes);
        await _pondRepo.SaveChangesAsync();

        var cacheKey = $"weather:{guildId}";
        _cache.Set(cacheKey, weather,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes)
            });
    }

    /// <summary>
    /// Áp dụng weather modifier lên drop weight table.
    /// Trả về (rareMod, legendaryMod, stormBreakChance).
    /// </summary>
    public static (double rareMod, double legendaryMod, double stormBreakChance)
        GetModifiers(PondWeather weather) => weather switch
    {
        PondWeather.Rainy  => (0.15, 0.05, 0.0),
        PondWeather.Stormy => (0.0,  0.05, 0.20),
        _                  => (0.0,  0.0,  0.0)   // Sunny: no modifier
    };

    public static string GetWeatherEmoji(PondWeather weather) => weather switch
    {
        PondWeather.Rainy  => "🌧️",
        PondWeather.Stormy => "⛈️",
        _                  => "☀️"
    };
}
