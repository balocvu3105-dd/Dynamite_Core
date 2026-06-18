// src/Dynamite.Modules.Economy/Services/PondService.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Common;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public record PondStatus(
    int CurrentFish,
    int MaxFish,
    bool IsEmpty,
    DateTime? ResetAvailableAt,
    PondWeather Weather,
    DateTime WeatherExpiresAt);

/// <summary>
/// Quản lý pool cá của guild (DB-backed, persistent qua restart).
/// Capacity mặc định 5000 → hết → lock 30 phút → reset.
/// </summary>
public class PondService
{
    private static readonly TimeSpan ResetCooldown = TimeSpan.FromMinutes(30);

    private readonly IPondRepository _pondRepo;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<PondService> _logger;

    public PondService(
        IPondRepository pondRepo,
        DiscordSocketClient client,
        ILogger<PondService> logger)
    {
        _pondRepo = pondRepo;
        _client = client;
        _logger = logger;
    }

    public async Task<PondStatus> GetStatusAsync(ulong guildId)
    {
        var pond = await _pondRepo.GetOrCreateAsync(guildId);
        return ToStatus(pond);
    }

    /// <summary>
    /// Thử trừ 1 cá từ bể.
    /// Returns (success, pond) — false nếu bể đang cạn và cooldown chưa hết.
    /// </summary>
    public async Task<ServiceResult<PondStatus>> TryConsumeAsync(ulong guildId)
    {
        var pond = await _pondRepo.GetOrCreateAsync(guildId);

        // Nếu bể đang trong cooldown reset
        if (pond.IsEmpty)
        {
            // Kiểm tra nếu đã đủ 30 phút → tự reset
            if (pond.CanReset)
            {
                await ResetPondAsync(pond, guildId);
            }
            else
            {
                var remaining = pond.ResetAvailableAt!.Value - DateTime.UtcNow;
                var ts = new DateTimeOffset(pond.ResetAvailableAt.Value).ToUnixTimeSeconds();
                return ServiceResult<PondStatus>.Fail(
                    $"🪣 Bể cá đang trống! Cá mới sẽ về <t:{ts}:R>.");
            }
        }

        pond.CurrentFish--;

        if (pond.CurrentFish <= 0)
        {
            pond.CurrentFish = 0;
            pond.DepletedAt = DateTime.UtcNow;
            pond.ResetAvailableAt = DateTime.UtcNow.Add(ResetCooldown);
            await _pondRepo.SaveChangesAsync();

            _logger.LogInformation("[Guild {GuildId}] Pond depleted — reset at {ResetAt:HH:mm} UTC",
                guildId, pond.ResetAvailableAt);

            // Thông báo vào fishing channel nếu có
            _ = Task.Run(() => NotifyPondDepletedAsync(guildId, pond.FishingChannelId, pond.ResetAvailableAt.Value));
        }
        else
        {
            await _pondRepo.SaveChangesAsync();
        }

        return ServiceResult<PondStatus>.Ok(ToStatus(pond));
    }

    /// <summary>
    /// Admin override: nạp lại bể ngay lập tức.
    /// - CurrentFish = MaxFish
    /// - Xóa DepletedAt và ResetAvailableAt (cancel timer nếu đang đếm ngược)
    /// - KHÔNG gửi notification, KHÔNG set timer mới
    /// → Khi bể cạn lần tới, chu kỳ 30 phút bình thường sẽ chạy như cũ.
    /// </summary>
    public async Task<PondStatus> AdminRefillAsync(ulong guildId)
    {
        var pond = await _pondRepo.GetOrCreateAsync(guildId);

        pond.CurrentFish      = pond.MaxFish;
        pond.DepletedAt       = null;
        pond.ResetAvailableAt = null;

        await _pondRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[Guild {GuildId}] Pond admin-refilled → {MaxFish} fish (timer cleared)",
            guildId, pond.MaxFish);

        return ToStatus(pond);
    }

    /// <summary>
    /// Admin override: cộng thêm một lượng cá cụ thể vào bể, không vượt MaxFish.
    /// KHÔNG clear timer — nếu bể đang trong cooldown, timer vẫn chạy.
    /// Dùng khi admin muốn "tiếp tế" một phần mà không reset hoàn toàn.
    /// </summary>
    public async Task<PondStatus> AdminPartialRefillAsync(ulong guildId, int amount)
    {
        var pond = await _pondRepo.GetOrCreateAsync(guildId);

        pond.CurrentFish = Math.Min(pond.CurrentFish + amount, pond.MaxFish);

        // Nếu bể không còn empty sau khi cộng → xóa trạng thái depleted
        if (!pond.IsEmpty)
        {
            pond.DepletedAt       = null;
            pond.ResetAvailableAt = null; // cancel timer nếu đang chạy
        }

        await _pondRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[Guild {GuildId}] Pond partial refill +{Amount} → {Current}/{Max}",
            guildId, amount, pond.CurrentFish, pond.MaxFish);

        return ToStatus(pond);
    }

    private async Task ResetPondAsync(GuildPond pond, ulong guildId)
    {
        pond.CurrentFish = pond.MaxFish;
        pond.DepletedAt = null;
        pond.ResetAvailableAt = null;
        await _pondRepo.SaveChangesAsync();

        _logger.LogInformation("[Guild {GuildId}] Pond reset → {MaxFish} fish", guildId, pond.MaxFish);

        _ = Task.Run(() => NotifyPondResetAsync(guildId, pond.FishingChannelId, pond.MaxFish));
    }

    private async Task NotifyPondDepletedAsync(ulong guildId, ulong? channelId, DateTime resetAt)
    {
        if (channelId is null) return;
        var channel = _client.GetGuild(guildId)?.GetTextChannel(channelId.Value);
        if (channel is null) return;

        var ts = new DateTimeOffset(resetAt).ToUnixTimeSeconds();
        var embed = new EmbedBuilder()
            .WithTitle("🪣 Bể cá đã cạn kiệt!")
            .WithDescription(
                $"Tất cả cá đã bị câu hết.\n" +
                $"Bể sẽ được làm mới <t:{ts}:R> với **{5000}** con cá mới.")
            .WithColor(new Color(0xED4245))
            .WithFooter("Hãy quay lại sau!")
            .Build();

        try { await channel.SendMessageAsync(embed: embed); }
        catch { /* channel bị xóa hoặc mất quyền */ }
    }

    private async Task NotifyPondResetAsync(ulong guildId, ulong? channelId, int maxFish)
    {
        if (channelId is null) return;
        var channel = _client.GetGuild(guildId)?.GetTextChannel(channelId.Value);
        if (channel is null) return;

        var embed = new EmbedBuilder()
            .WithTitle("🐟 Bể cá đã được làm mới!")
            .WithDescription($"**{maxFish:N0}** con cá mới đã bơi vào bể!\nHãy vào câu ngay!")
            .WithColor(new Color(0x57F287))
            .Build();

        try { await channel.SendMessageAsync(embed: embed); }
        catch { /* ignore */ }
    }

    private static PondStatus ToStatus(GuildPond pond) => new(
        pond.CurrentFish,
        pond.MaxFish,
        pond.IsEmpty,
        pond.ResetAvailableAt,
        pond.CurrentWeather,
        pond.WeatherExpiresAt);
}
