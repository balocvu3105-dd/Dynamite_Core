// src/Dynamite.Modules.Economy/Services/WeatherForecastService.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Quản lý embed dự báo thời tiết trong WeatherChannelId.
/// Tự động edit embed hiện có khi thời tiết thay đổi (ForceWeather hoặc advance).
/// Message ID lưu vào GuildConfig.WeatherForecastMessageId.
/// </summary>
public class WeatherForecastService
{
    private readonly IGuildConfigRepository _configRepo;
    private readonly IPondRepository        _pondRepo;
    private readonly DiscordSocketClient    _discord;
    private readonly ILogger<WeatherForecastService> _logger;

    public WeatherForecastService(
        IGuildConfigRepository configRepo,
        IPondRepository        pondRepo,
        DiscordSocketClient    discord,
        ILogger<WeatherForecastService> logger)
    {
        _configRepo = configRepo;
        _pondRepo   = pondRepo;
        _discord    = discord;
        _logger     = logger;
    }

    /// <summary>Set channel dự báo, post embed lần đầu và pin.</summary>
    public async Task<(bool ok, string message)> SetChannelAsync(
        ulong guildId, string guildName, ITextChannel channel)
    {
        var config = await _configRepo.GetOrCreateAsync(guildId, guildName);

        // Xóa message cũ nếu có
        if (config.WeatherForecastMessageId.HasValue)
            await TryDeleteOldMessageAsync(guildId, config);

        config.WeatherChannelId          = channel.Id;
        config.WeatherForecastMessageId  = null;
        await _configRepo.SaveChangesAsync();

        var msgId = await PostForecastAsync(guildId, channel);
        if (msgId is null)
            return (false, "❌ Không thể post embed dự báo thời tiết.");

        config.WeatherForecastMessageId = msgId;
        await _configRepo.SaveChangesAsync();

        try
        {
            var msg = await channel.GetMessageAsync(msgId.Value) as IUserMessage;
            if (msg is not null) await msg.PinAsync();
        }
        catch { /* pin failed */ }

        return (true, $"✅ Đã đặt {channel.Mention} làm kênh dự báo thời tiết.");
    }

    /// <summary>
    /// Cập nhật embed khi thời tiết thay đổi.
    /// Gọi từ ItemCommands sau khi dùng Phép Triệu Mưa, và từ WeatherService khi advance.
    /// Non-throwing.
    /// </summary>
    public async Task RefreshAsync(ulong guildId)
    {
        try
        {
            var config = await _configRepo.GetByGuildIdAsync(guildId);
            if (config?.WeatherChannelId is null) return;

            var guild   = _discord.GetGuild(guildId);
            var channel = guild?.GetTextChannel(config.WeatherChannelId.Value);
            if (channel is null) return;

            if (config.WeatherForecastMessageId.HasValue)
            {
                var msg = await channel.GetMessageAsync(config.WeatherForecastMessageId.Value) as IUserMessage;
                if (msg is not null)
                {
                    var embed = await BuildForecastEmbedAsync(guildId);
                    await msg.ModifyAsync(p => p.Embed = embed);
                    return;
                }
            }

            // Message bị xóa — post lại
            var newId = await PostForecastAsync(guildId, channel);
            if (newId.HasValue)
            {
                config.WeatherForecastMessageId = newId;
                await _configRepo.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[WeatherForecast] Refresh failed for guild {GuildId}", guildId);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ulong?> PostForecastAsync(ulong guildId, ITextChannel channel)
    {
        try
        {
            var embed = await BuildForecastEmbedAsync(guildId);
            var msg   = await channel.SendMessageAsync(embed: embed);
            return msg.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[WeatherForecast] Post failed for guild {GuildId}", guildId);
            return null;
        }
    }

    private async Task<Embed> BuildForecastEmbedAsync(ulong guildId)
    {
        var pond = await _pondRepo.GetOrCreateAsync(guildId);
        var now  = DateTime.UtcNow;

        var weather     = pond.CurrentWeather;
        var expiresAt   = pond.WeatherExpiresAt;
        var isExpired   = now >= expiresAt;

        // Nếu đã hết hạn, tính thời tiết tiếp theo (preview)
        if (isExpired)
        {
            var cycle = new[] { PondWeather.Sunny, PondWeather.Rainy, PondWeather.Stormy };
            var idx   = Array.IndexOf(cycle, weather);
            weather   = cycle[(idx + 1) % cycle.Length];
            expiresAt = now.AddHours(2);
        }

        var (emoji, weatherName, effect, color) = weather switch
        {
            PondWeather.Rainy  => ("🌧️", "Mưa",     "+15% Rare | +5% Legendary",            new Color(0x3498DB)),
            PondWeather.Stormy => ("⛈️", "Giông Bão", "+5% Legendary | 20% đứt cước mất lượt", new Color(0x8E44AD)),
            _                  => ("☀️", "Nắng",     "Tỉ lệ bình thường",                      new Color(0xF39C12)),
        };

        var expiresUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        var nextWeather = weather switch
        {
            PondWeather.Sunny  => "🌧️ Mưa",
            PondWeather.Rainy  => "⛈️ Giông Bão",
            _                  => "☀️ Nắng"
        };

        return new EmbedBuilder()
            .WithTitle($"{emoji} Dự Báo Thời Tiết Bể Cá")
            .WithColor(color)
            .AddField("Thời tiết hiện tại", $"**{emoji} {weatherName}**", inline: true)
            .AddField("Hiệu ứng", effect, inline: true)
            .AddField("​", "​", inline: true)
            .AddField("Đổi thời tiết lúc", $"<t:{expiresUnix}:F>  (<t:{expiresUnix}:R>)", inline: false)
            .AddField("Thời tiết tiếp theo", nextWeather, inline: true)
            .AddField("Cá còn trong bể", $"🐟 **{pond.CurrentFish:N0}** / {pond.MaxFish:N0}", inline: true)
            .WithFooter("Dùng 🌧️ Phép Triệu Mưa để thay đổi thời tiết • /item use <tên>")
            .WithCurrentTimestamp()
            .Build();
    }

    private async Task TryDeleteOldMessageAsync(ulong guildId, Core.Entities.GuildConfig config)
    {
        try
        {
            if (config.WeatherChannelId is null || config.WeatherForecastMessageId is null) return;
            var guild   = _discord.GetGuild(guildId);
            var channel = guild?.GetTextChannel(config.WeatherChannelId.Value);
            if (channel is null) return;
            var msg = await channel.GetMessageAsync(config.WeatherForecastMessageId.Value);
            if (msg is not null) await msg.DeleteAsync();
        }
        catch { /* ignore */ }
    }
}
