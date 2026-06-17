// src/Dynamite.Modules.Economy/Services/AutoFishScheduler.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// BackgroundService chạy auto-câu cho tất cả user/admin đang có session hoạt động.
///
/// Tick mỗi 27 giây (cooldown DB là 25s → buffer 2s):
///   • User mode  (AutoFishSellAll = true):  bán tất cả, post embed kết quả vào channel.
///   • Admin mode (AutoFishSellAll = false): giữ Rare+, ẩn hoàn toàn (không post gì).
///
/// Channel được lưu trong AutoFishChannelId tại thời điểm user bấm /fish-auto buy.
/// Nếu channel đã xoá hoặc bot không có quyền → bỏ qua lặng lẽ.
/// </summary>
public sealed class AutoFishScheduler : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(27);

    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly DiscordSocketClient       _discord;
    private readonly ILogger<AutoFishScheduler> _logger;

    public AutoFishScheduler(
        IServiceScopeFactory       scopeFactory,
        DiscordSocketClient        discord,
        ILogger<AutoFishScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _discord      = discord;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[AutoFish] Scheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AutoFish] Unhandled error in tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("[AutoFish] Scheduler stopping.");
    }

    private async Task TickAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var profileRepo    = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
        var fishingService = scope.ServiceProvider.GetRequiredService<FishingService>();
        var bagService     = scope.ServiceProvider.GetRequiredService<FishBagService>();

        var activeProfiles = await profileRepo.GetAllActiveAutoFishProfilesAsync();

        if (activeProfiles.Count == 0) return;

        _logger.LogDebug("[AutoFish] Ticking {Count} active session(s).", activeProfiles.Count);

        foreach (var profile in activeProfiles)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await ProcessUserAsync(profile, fishingService, bagService);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[AutoFish] Error processing user {UserId} in guild {GuildId}.",
                    profile.UserId, profile.GuildId);
            }

            // Nhường CPU giữa các user để tránh spike
            await Task.Delay(200, ct);
        }
    }

    private async Task ProcessUserAsync(
        UserFishingProfile profile,
        FishingService     fishingService,
        FishBagService     bagService)
    {
        var guildId = profile.GuildId;
        var userId  = profile.UserId;
        var sellAll = profile.AutoFishSellAll;

        // Lấy display name từ Discord cache (nickname > username > userId)
        var username = _discord.GetGuild(guildId)?.GetUser(userId)?.DisplayName
                    ?? _discord.GetUser(userId)?.Username
                    ?? userId.ToString();

        var (success, _, result) = await fishingService.FishAsync(guildId, userId);

        // ── Admin mode: bán Common/Uncommon + post embed (không có countdown) ─
        if (!sellAll)
        {
            if (success && result is not null
                && result.Catch.Rarity is "Common" or "Uncommon")
            {
                await bagService.SellByRarityAsync(guildId, userId, result.Catch.Rarity);
            }

            if (success && result is not null && profile.AutoFishChannelId != 0)
                await PostAdminEmbedAsync(profile, result, username);

            return;
        }

        // ── User mode: bán tất + post embed ──────────────────────────────────
        if (!success || result is null) return;

        await bagService.SellByRarityAsync(guildId, userId, result.Catch.Rarity);

        if (profile.AutoFishChannelId != 0)
            await PostUserEmbedAsync(profile, result, username);
    }

    private async Task PostUserEmbedAsync(UserFishingProfile profile, FishResult result, string username)
    {
        try
        {
            if (_discord.GetChannel(profile.AutoFishChannelId) is not IMessageChannel channel)
                return;

            var embed = EconomyEmbedBuilder.BuildAutoFishEmbed(result, profile.AutoFishExpiresAt!.Value, username);
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[AutoFish] Failed to post user embed to channel {Id}: {Msg}",
                profile.AutoFishChannelId, ex.Message);
        }
    }

    private async Task PostAdminEmbedAsync(UserFishingProfile profile, FishResult result, string username)
    {
        try
        {
            if (_discord.GetChannel(profile.AutoFishChannelId) is not IMessageChannel channel)
                return;

            var embed = EconomyEmbedBuilder.BuildAdminAutoFishEmbed(result, username);
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[AutoFish] Failed to post admin embed to channel {Id}: {Msg}",
                profile.AutoFishChannelId, ex.Message);
        }
    }
}
