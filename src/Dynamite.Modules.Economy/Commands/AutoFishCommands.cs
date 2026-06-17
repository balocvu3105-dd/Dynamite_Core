// src/Dynamite.Modules.Economy/Commands/AutoFishCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Microsoft.Extensions.Logging;

/// <summary>
/// /auto-fish — Session câu cá tự động dành riêng cho Admin/Owner.
///
/// Thiết kế:
/// - Session có thời hạn (max 8 giờ), lưu AutoFishExpiresAt trên profile
/// - Bot câu mỗi 35 giây (AutoFishScheduler), auto-sell Common/Uncommon, giữ Rare+
/// - Không tốn item hay coin — privilege cho Admin/Owner để flex bộ sưu tập
/// </summary>
[RequireContext(ContextType.Guild)]
[DefaultMemberPermissions(GuildPermission.Administrator)]
[Group("auto-fish", "Auto câu cá tự động (Admin/Owner only)")]
public class AutoFishCommands : InteractionModuleBase<SocketInteractionContext>
{
    private const int MaxHours = 8;

    private readonly IUserProfileRepository _profileRepo;
    private readonly IGuildConfigRepository _configRepo;
    private readonly ILogger<AutoFishCommands> _logger;

    public AutoFishCommands(
        IUserProfileRepository profileRepo,
        IGuildConfigRepository configRepo,
        ILogger<AutoFishCommands> logger)
    {
        _profileRepo = profileRepo;
        _configRepo  = configRepo;
        _logger      = logger;
    }

    // ── /auto-fish start [hours] ─────────────────────────────────────────────

    [SlashCommand("start", "Bắt đầu session auto câu cá")]
    public async Task StartAsync(
        [Summary("hours", "Số giờ (1-8, mặc định 4)")]
        [MinValue(1)][MaxValue(MaxHours)]
        int hours = 4)
    {
        if (!IsAdminOrOwner(Context))
        {
            await RespondAsync(
                "❌ Lệnh này chỉ dành cho **Admin** hoặc **Owner** server.",
                ephemeral: true);
            return;
        }

        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        var now = DateTime.UtcNow;

        // Nếu đang có session chưa hết → gia hạn
        var isRenew = profile.AutoFishExpiresAt.HasValue && profile.AutoFishExpiresAt > now;

        // Kết quả luôn post vào FishingChannelId nếu đã set, fallback về channel hiện tại
        var guildConfig = await _configRepo.GetByGuildIdAsync(Context.Guild.Id);
        var fishChannel = guildConfig?.FishingChannelId ?? Context.Channel.Id;

        profile.AutoFishExpiresAt  = now.AddHours(hours);
        profile.AutoFishSellAll    = false; // admin mode: giữ Rare+
        profile.AutoFishChannelId  = fishChannel;
        await _profileRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[AutoFish] {UserId} started session {Hours}h in guild {GuildId}",
            Context.User.Id, hours, Context.Guild.Id);

        var expiresUnix = ((DateTimeOffset)profile.AutoFishExpiresAt.Value).ToUnixTimeSeconds();

        var embed = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithTitle(isRenew ? "🔄 Auto-Fish Gia Hạn" : "🎣 Auto-Fish Bắt Đầu!")
            .WithDescription(
                $"Bot sẽ tự câu cho bạn mỗi **35 giây**.\n" +
                $"**Common/Uncommon** sẽ tự bán.\n" +
                $"**Rare+** sẽ được giữ vào túi cá.\n\n" +
                $"⏰ Hết hạn: <t:{expiresUnix}:R> (<t:{expiresUnix}:T>)")
            .WithFooter($"Session {hours}h | Dùng /auto-fish stop để dừng sớm")
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── /auto-fish stop ──────────────────────────────────────────────────────

    [SlashCommand("stop", "Dừng session auto câu cá")]
    public async Task StopAsync()
    {
        if (!IsAdminOrOwner(Context))
        {
            await RespondAsync(
                "❌ Lệnh này chỉ dành cho **Admin** hoặc **Owner** server.",
                ephemeral: true);
            return;
        }

        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        if (profile.AutoFishExpiresAt is null || profile.AutoFishExpiresAt <= DateTime.UtcNow)
        {
            await FollowupAsync("ℹ️ Bạn không có session auto-fish nào đang chạy.", ephemeral: true);
            return;
        }

        profile.AutoFishExpiresAt = null;
        profile.AutoFishSellAll   = false;
        await _profileRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[AutoFish] {UserId} stopped session in guild {GuildId}",
            Context.User.Id, Context.Guild.Id);

        await FollowupAsync("⛔ Session auto-fish đã dừng.", ephemeral: true);
    }

    // ── /auto-fish status ────────────────────────────────────────────────────

    [SlashCommand("status", "Xem trạng thái session auto câu cá")]
    public async Task StatusAsync()
    {
        if (!IsAdminOrOwner(Context))
        {
            await RespondAsync(
                "❌ Lệnh này chỉ dành cho **Admin** hoặc **Owner** server.",
                ephemeral: true);
            return;
        }

        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        var now = DateTime.UtcNow;
        var isActive = profile.AutoFishExpiresAt.HasValue && profile.AutoFishExpiresAt > now;

        EmbedBuilder embed;

        if (isActive)
        {
            var expiresUnix = ((DateTimeOffset)profile.AutoFishExpiresAt!.Value).ToUnixTimeSeconds();
            embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("🎣 Auto-Fish Đang Chạy")
                .WithDescription(
                    $"Hết hạn: <t:{expiresUnix}:R>\n" +
                    $"Dùng `/auto-fish stop` để dừng sớm.");
        }
        else
        {
            embed = new EmbedBuilder()
                .WithColor(Color.LightGrey)
                .WithTitle("⛔ Auto-Fish Không Hoạt Động")
                .WithDescription("Dùng `/auto-fish start [hours]` để bắt đầu.");
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    // ── /auto-fish stop-user ─────────────────────────────────────────────────

    [SlashCommand("stop-user", "Dừng session auto câu cá của một user bất kỳ (Admin)")]
    public async Task StopUserAsync(
        [Summary("user", "User cần dừng session")] IUser target)
    {
        if (!IsAdminOrOwner(Context))
        {
            await RespondAsync(
                "❌ Lệnh này chỉ dành cho **Admin** hoặc **Owner** server.",
                ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(Context.Guild.Id, target.Id);
        var now     = DateTime.UtcNow;

        if (profile.AutoFishExpiresAt is null || profile.AutoFishExpiresAt <= now)
        {
            await FollowupAsync(
                $"ℹ️ **{target.Username}** không có session auto-fish nào đang chạy.",
                ephemeral: true);
            return;
        }

        profile.AutoFishExpiresAt = null;
        profile.AutoFishSellAll   = false;
        profile.AutoFishPaused    = false;
        await _profileRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[AutoFish] Admin {AdminId} force-stopped session of {UserId} in guild {GuildId}",
            Context.User.Id, target.Id, Context.Guild.Id);

        await FollowupAsync(
            $"⛔ Đã dừng session auto-fish của **{target.Username}**.",
            ephemeral: true);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static bool IsAdminOrOwner(SocketInteractionContext ctx)
    {
        if (ctx.User is not SocketGuildUser guildUser) return false;
        return guildUser.GuildPermissions.Administrator
            || ctx.Guild.OwnerId == guildUser.Id;
    }
}
