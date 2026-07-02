// src/Dynamite.Modules.Economy/Services/WeatherChangeNotifier.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service chạy mỗi 60 giây.
/// Khi phát hiện thời tiết bể cá của một guild đã hết hạn:
///   1. Advance weather (Sunny → Rainy → Stormy → Sunny)
///   2. Gửi thông báo mention FishingRoleId vào WeatherChannelId
///   3. Refresh embed dự báo thời tiết
/// </summary>
public sealed class WeatherChangeNotifier : BackgroundService
{
    private static readonly TimeSpan _interval    = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _weatherDuration = TimeSpan.FromHours(2);
    private static readonly PondWeather[] _cycle  =
        [PondWeather.Sunny, PondWeather.Rainy, PondWeather.Stormy];

    private readonly IServiceScopeFactory           _scopeFactory;
    private readonly DiscordSocketClient            _discord;
    private readonly WeatherForecastService         _forecast;
    private readonly IBotStatusProvider             _botStatus;
    private readonly ILogger<WeatherChangeNotifier> _logger;

    public WeatherChangeNotifier(
        IServiceScopeFactory           scopeFactory,
        DiscordSocketClient            discord,
        WeatherForecastService         forecast,
        IBotStatusProvider             botStatus,
        ILogger<WeatherChangeNotifier> logger)
    {
        _scopeFactory = scopeFactory;
        _discord      = discord;
        _forecast     = forecast;
        _botStatus    = botStatus;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[WeatherNotifier] Started.");

        // Chờ bot ready xong
        while (!_botStatus.IsReady && !stoppingToken.IsCancellationRequested)
            await Task.Delay(5_000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await CheckAllGuildsAsync(stoppingToken);
        }
    }

    private async Task CheckAllGuildsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var pondRepo   = scope.ServiceProvider.GetRequiredService<IPondRepository>();
        var configRepo = scope.ServiceProvider.GetRequiredService<IGuildConfigRepository>();

        var ponds = await pondRepo.GetAllAsync();
        var now   = DateTime.UtcNow;

        foreach (var pond in ponds)
        {
            if (ct.IsCancellationRequested) break;
            if (now < pond.WeatherExpiresAt) continue; // chưa hết hạn

            try
            {
                await AdvanceAndNotifyAsync(pond, configRepo, pondRepo, now);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WeatherNotifier] Failed for guild {GuildId}", pond.GuildId);
            }
        }
    }

    private async Task AdvanceAndNotifyAsync(
        GuildPond              pond,
        IGuildConfigRepository configRepo,
        IPondRepository        pondRepo,
        DateTime               now)
    {
        // 1. Advance weather
        var oldWeather  = pond.CurrentWeather;
        var idx         = Array.IndexOf(_cycle, oldWeather);
        var newWeather  = _cycle[(idx + 1) % _cycle.Length];

        pond.CurrentWeather   = newWeather;
        pond.WeatherExpiresAt = now.Add(_weatherDuration);
        await pondRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[WeatherNotifier] Guild {GuildId}: {Old} → {New}",
            pond.GuildId, oldWeather, newWeather);

        // 2. Refresh embed dự báo
        await _forecast.RefreshAsync(pond.GuildId);

        // 3. Gửi thông báo vào WeatherChannel
        var config = await configRepo.GetByGuildIdAsync(pond.GuildId);
        if (config?.WeatherChannelId is null) return;

        var guild   = _discord.GetGuild(pond.GuildId);
        var channel = guild?.GetTextChannel(config.WeatherChannelId.Value);
        if (channel is null) return;

        var (emoji, name, effect) = newWeather switch
        {
            PondWeather.Rainy  => ("🌧️", "Mưa",      "+15% Rare · +5% Legendary · −10% hụt cần"),
            PondWeather.Stormy => ("⛈️", "Giông Bão", "+5% Legendary · ×1.25 giá bán · +8% hụt cần"),
            _                  => ("☀️", "Nắng",      "Tỉ lệ bình thường"),
        };

        var nextWeather = newWeather switch
        {
            PondWeather.Sunny  => "🌧️ Mưa",
            PondWeather.Rainy  => "⛈️ Giông Bão",
            _                  => "☀️ Nắng"
        };

        var expiresUnix = new DateTimeOffset(pond.WeatherExpiresAt).ToUnixTimeSeconds();

        var embed = new EmbedBuilder()
            .WithTitle($"{emoji} Thời Tiết Bể Cá Vừa Thay Đổi!")
            .WithColor(newWeather switch
            {
                PondWeather.Rainy  => new Color(0x3498DB),
                PondWeather.Stormy => new Color(0x8E44AD),
                _                  => new Color(0xF39C12)
            })
            .AddField("Thời tiết mới",  $"**{emoji} {name}**",  inline: true)
            .AddField("Hiệu ứng",       effect,                  inline: true)
            .AddField("​", "​",                                   inline: true)
            .AddField("Kéo dài đến",    $"<t:{expiresUnix}:F>  (<t:{expiresUnix}:R>)", inline: false)
            .AddField("Tiếp theo",       nextWeather,             inline: true)
            .WithFooter("Dùng /shop use Phép Triệu Mưa để đổi thời tiết thủ công")
            .WithCurrentTimestamp()
            .Build();

        // Mention role nếu có
        var mention = config.FishingRoleId.HasValue
            ? $"<@&{config.FishingRoleId.Value}> "
            : string.Empty;

        await channel.SendMessageAsync(text: mention, embed: embed);
    }
}
