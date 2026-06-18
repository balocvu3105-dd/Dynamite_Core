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
///   • Câu cá → lưu vào túi (KHÔNG tự bán).
///   • Khi túi đầy → tự động pause session + ping user vào channel.
///   • User mode (AutoFishSellAll = true):  post embed kết quả vào channel.
///   • Admin mode (AutoFishSellAll = false): post embed kiểu admin (không có countdown).
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
        var specialPool    = scope.ServiceProvider.GetRequiredService<SpecialPoolService>();
        var configRepo     = scope.ServiceProvider.GetRequiredService<IGuildConfigRepository>();

        var activeProfiles = await profileRepo.GetAllActiveAutoFishProfilesAsync();

        if (activeProfiles.Count == 0) return;

        _logger.LogDebug("[AutoFish] Ticking {Count} active session(s).", activeProfiles.Count);

        // Cache guild fishing-enabled state per tick (tránh query DB lặp cho cùng guild)
        var guildEnabledCache = new Dictionary<ulong, bool>();

        foreach (var profile in activeProfiles)
        {
            if (ct.IsCancellationRequested) break;

            // Bãi câu đóng → bỏ qua toàn bộ user trong guild đó
            if (!guildEnabledCache.TryGetValue(profile.GuildId, out var enabled))
            {
                var cfg = await configRepo.GetByGuildIdAsync(profile.GuildId);
                enabled = cfg?.FishingEnabled ?? true;
                guildEnabledCache[profile.GuildId] = enabled;
            }

            if (!enabled)
            {
                _logger.LogDebug("[AutoFish] Fishing closed in guild {GuildId} — skipping user {UserId}.",
                    profile.GuildId, profile.UserId);
                continue;
            }

            try
            {
                await ProcessUserAsync(profile, fishingService, specialPool, profileRepo);
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
        UserFishingProfile     profile,
        FishingService         fishingService,
        SpecialPoolService     specialPoolService,
        IUserProfileRepository profileRepo)
    {
        if (profile.AutoFishPaused) return;

        var guildId = profile.GuildId;
        var userId  = profile.UserId;
        var isAdmin = !profile.AutoFishSellAll;

        // Lấy display name từ Discord cache (nickname > username > userId)
        var username = _discord.GetGuild(guildId)?.GetUser(userId)?.DisplayName
                    ?? _discord.GetUser(userId)?.Username
                    ?? userId.ToString();

        // ── Route: Special Pool hay bể thường? ───────────────────────────────
        var now = DateTime.UtcNow;
        var specialPoolActive = profile.AutoFishSpecialPoolId.HasValue
                             && profile.AutoFishSpecialPoolExpiresAt.HasValue
                             && profile.AutoFishSpecialPoolExpiresAt > now;

        if (specialPoolActive)
        {
            await ProcessSpecialPoolAsync(
                profile, specialPoolService, profileRepo, guildId, userId, username);
        }
        else if (profile.AutoFishSpecialPoolId.HasValue)
        {
            // Vé hết hạn 2h → clear fields + notify + tiếp tục câu bể thường tick này
            var freshProfile = await profileRepo.GetOrCreateFishingAsync(guildId, userId);
            freshProfile.AutoFishSpecialPoolId        = null;
            freshProfile.AutoFishSpecialPoolExpiresAt = null;
            await profileRepo.SaveChangesAsync();

            if (profile.AutoFishChannelId != 0)
                await PostSpecialPoolFallbackAsync(profile, username, "Vé Pool Đặc Biệt đã hết hạn (2 tiếng).");

            await ProcessRegularAsync(
                profile, fishingService, profileRepo, guildId, userId, isAdmin, username);
        }
        else
        {
            await ProcessRegularAsync(
                profile, fishingService, profileRepo, guildId, userId, isAdmin, username);
        }
    }

    // ── Regular fish ──────────────────────────────────────────────────────────

    private async Task ProcessRegularAsync(
        UserFishingProfile     profile,
        FishingService         fishingService,
        IUserProfileRepository profileRepo,
        ulong guildId, ulong userId, bool isAdmin, string username)
    {
        var (success, reason, result) = await fishingService.FishAsync(guildId, userId, useBait: profile.AutoFishUseBait);

        if (!success)
        {
            // Bỏ qua cooldown (không nên xảy ra với tick 27s vs cooldown 25s)
            // Hiện miss/escape và các lý do khác ra channel
            if (!string.IsNullOrEmpty(reason)
                && !reason.StartsWith("⏳")
                && profile.AutoFishChannelId != 0)
            {
                await PostMissEscapeEmbedAsync(profile, reason, username);
            }
            return;
        }

        if (result is null) return;

        // ── Túi đầy → auto-pause + notify ────────────────────────────────────
        if (!result.SavedToBag)
        {
            var freshProfile = await profileRepo.GetOrCreateFishingAsync(guildId, userId);
            freshProfile.AutoFishPaused = true;
            await profileRepo.SaveChangesAsync();

            _logger.LogInformation(
                "[AutoFish] Bag full — paused session for user {UserId} in guild {GuildId}",
                userId, guildId);

            if (profile.AutoFishChannelId != 0)
                await PostBagFullNotificationAsync(profile, username);

            return;
        }

        if (profile.AutoFishChannelId == 0) return;

        if (isAdmin)
            await PostAdminEmbedAsync(profile, result, username);
        else
            await PostUserEmbedAsync(profile, result, username);
    }

    // ── Special Pool fish ─────────────────────────────────────────────────────

    private async Task ProcessSpecialPoolAsync(
        UserFishingProfile     profile,
        SpecialPoolService     specialPoolService,
        IUserProfileRepository profileRepo,
        ulong guildId, ulong userId, string username)
    {
        var poolId = profile.AutoFishSpecialPoolId!.Value;

        var (success, reason, result) =
            await specialPoolService.FishSpecialAsync(guildId, userId, poolId);

        if (!success)
        {
            // Pool không còn active → tự switch về bể thường + notify
            _logger.LogInformation(
                "[AutoFish] Special pool failed ({Reason}) for user {UserId} — switching to regular",
                reason, userId);

            var freshProfile = await profileRepo.GetOrCreateFishingAsync(guildId, userId);
            freshProfile.AutoFishSpecialPoolId        = null;
            freshProfile.AutoFishSpecialPoolExpiresAt = null;
            await profileRepo.SaveChangesAsync();

            if (profile.AutoFishChannelId != 0)
                await PostSpecialPoolFallbackAsync(profile, username, reason);

            return;
        }

        // ── Túi đầy → pause ───────────────────────────────────────────────────
        if (result is not null && !result.SavedToBag)
        {
            var freshProfile = await profileRepo.GetOrCreateFishingAsync(guildId, userId);
            freshProfile.AutoFishPaused = true;
            await profileRepo.SaveChangesAsync();

            if (profile.AutoFishChannelId != 0)
                await PostBagFullNotificationAsync(profile, username);

            return;
        }

        if (profile.AutoFishChannelId == 0 || result is null) return;

        await PostSpecialPoolEmbedAsync(profile, result, username);
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

    private async Task PostBagFullNotificationAsync(UserFishingProfile profile, string username)
    {
        try
        {
            if (_discord.GetChannel(profile.AutoFishChannelId) is not IMessageChannel channel)
                return;

            var embed = new EmbedBuilder()
                .WithColor(new Color(0xE67E22))
                .WithTitle("🎒 Túi Cá Đầy — Auto-Fish Tạm Dừng!")
                .WithDescription(
                    $"**{username}** — Túi cá đã đầy!\n\n" +
                    "Bot đã tự động **tạm dừng** session auto câu cá.\n" +
                    "Hãy bán bớt cá bằng `/bag sell-all`, rồi dùng `/fish-auto resume` để tiếp tục.")
                .WithFooter("Auto-Fish đã tạm dừng tự động • Timer vẫn đang chạy")
                .WithCurrentTimestamp()
                .Build();

            await channel.SendMessageAsync($"<@{profile.UserId}>", embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[AutoFish] Failed to post bag-full notification to channel {Id}: {Msg}",
                profile.AutoFishChannelId, ex.Message);
        }
    }

    private async Task PostSpecialPoolEmbedAsync(
        UserFishingProfile profile, SpecialFishResult result, string username)
    {
        try
        {
            if (_discord.GetChannel(profile.AutoFishChannelId) is not IMessageChannel channel)
                return;

            var embed = EconomyEmbedBuilder.BuildAutoSpecialFishEmbed(result, profile.AutoFishExpiresAt!.Value, username);
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[AutoFish] Failed to post special pool embed to channel {Id}: {Msg}",
                profile.AutoFishChannelId, ex.Message);
        }
    }

    private async Task PostSpecialPoolFallbackAsync(
        UserFishingProfile profile, string username, string? reason)
    {
        try
        {
            if (_discord.GetChannel(profile.AutoFishChannelId) is not IMessageChannel channel)
                return;

            var embed = new EmbedBuilder()
                .WithColor(new Color(0xE74C3C))
                .WithTitle("⭐ Pool Đặc Biệt Không Khả Dụng — Chuyển Về Bể Thường")
                .WithDescription(
                    $"**{username}** — Auto-fish pool đặc biệt bị gián đoạn.\n\n" +
                    $"**Lý do:** {reason ?? "Pool không còn hoạt động hoặc hết vé."}\n\n" +
                    "Bot đã tự động chuyển về **Bể Thường**.\n" +
                    "Mua thêm vé tại `/shop buy Vé Pool Đặc Biệt` hoặc chọn lại pool bằng `/fish-auto set-mode`.")
                .WithFooter("Auto-Fish Pool Mode • Đã fallback về bể thường")
                .WithCurrentTimestamp()
                .Build();

            await channel.SendMessageAsync($"<@{profile.UserId}>", embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[AutoFish] Failed to post special pool fallback to channel {Id}: {Msg}",
                profile.AutoFishChannelId, ex.Message);
        }
    }

    private async Task PostMissEscapeEmbedAsync(
        UserFishingProfile profile, string reason, string username)
    {
        try
        {
            if (_discord.GetChannel(profile.AutoFishChannelId) is not IMessageChannel channel)
                return;

            var embed = EconomyEmbedBuilder.BuildAutoFishMissEmbed(reason, username);
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[AutoFish] Failed to post miss/escape embed to channel {Id}: {Msg}",
                profile.AutoFishChannelId, ex.Message);
        }
    }
}
