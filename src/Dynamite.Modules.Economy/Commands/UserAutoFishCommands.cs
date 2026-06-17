// src/Dynamite.Modules.Economy/Commands/UserAutoFishCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Microsoft.Extensions.Logging;

/// <summary>
/// /fish-auto — Gói auto câu cá subscription cho user thường.
///
/// Cơ chế:
/// - Mua trực tiếp bằng coins, KHÔNG cần item trong shop
/// - Mỗi lần mua = 3 ngày auto câu, hết phải mua lại / gia hạn
/// - Giá leo thang theo số lần mua (lần 1 rẻ, càng về sau càng đắt)
/// - Bot câu mỗi 35 giây, bán toàn bộ cá câu được vào ví
///
/// Bảng giá (PriceTiers):
///   Lần 1:  5,000 coins   ← khuyến mãi đầu tiên
///   Lần 2:  12,000 coins
///   Lần 3:  25,000 coins
///   Lần 4:  45,000 coins
///   Lần 5+: 70,000 coins  ← giá trần
/// </summary>
[RequireContext(ContextType.Guild)]
[Group("fish-auto", "Gói auto câu cá (mua bằng coins, max 3 ngày/lần)")]
public class UserAutoFishCommands : InteractionModuleBase<SocketInteractionContext>
{
    // ── Cấu hình ─────────────────────────────────────────────────────────────

    /// <summary>Thời hạn mỗi lần mua: 3 ngày.</summary>
    private static readonly TimeSpan SessionDuration = TimeSpan.FromDays(3);

    /// <summary>
    /// Bảng giá theo lần mua (index = purchaseCount, index cuối là giá trần).
    /// Lần mua 5 trở đi đều trả giá tiers[^1].
    /// </summary>
    private static readonly long[] PriceTiers = [5_000, 12_000, 25_000, 45_000, 70_000];

    // ─────────────────────────────────────────────────────────────────────────

    private readonly IUserProfileRepository _profileRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IGuildConfigRepository _configRepo;
    private readonly ILogger<UserAutoFishCommands> _logger;

    public UserAutoFishCommands(
        IUserProfileRepository profileRepo,
        IWalletRepository walletRepo,
        IGuildConfigRepository configRepo,
        ILogger<UserAutoFishCommands> logger)
    {
        _profileRepo = profileRepo;
        _walletRepo  = walletRepo;
        _configRepo  = configRepo;
        _logger      = logger;
    }

    // ── /fish-auto buy ───────────────────────────────────────────────────────

    [SlashCommand("buy", "Mua / gia hạn gói auto câu cá 3 ngày")]
    public async Task BuyAsync()
    {
        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);
        var wallet = await _walletRepo.GetOrCreateAsync(
            Context.Guild.Id, Context.User.Id);

        var now   = DateTime.UtcNow;
        var price = GetPrice(profile.AutoFishPurchaseCount);

        // ── Kiểm tra túi tiền ────────────────────────────────────────────────
        if (wallet.Coins < price)
        {
            var needed = price - wallet.Coins;
            await FollowupAsync(
                $"❌ Không đủ coins!\n" +
                $"Cần **{price:N0}** coins — thiếu **{needed:N0}** coins.",
                ephemeral: true);
            return;
        }

        // ── Tính thời hạn ────────────────────────────────────────────────────
        // Gia hạn: nếu đang có session user (SellAll=true) thì cộng thêm từ thời điểm hiện tại hết hạn
        // Reset: nếu đang dùng admin session hoặc không có session
        DateTime newExpires;
        bool isRenew = false;

        if (profile.AutoFishExpiresAt.HasValue
            && profile.AutoFishExpiresAt > now
            && profile.AutoFishSellAll)
        {
            // Gia hạn từ điểm hết hạn hiện tại
            newExpires = profile.AutoFishExpiresAt.Value + SessionDuration;
            isRenew = true;
        }
        else
        {
            newExpires = now + SessionDuration;
        }

        // ── Trừ coins ────────────────────────────────────────────────────────
        wallet.Coins -= price;
        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId      = Context.Guild.Id,
            FromWalletId = wallet.Id,
            Amount       = price,
            Type         = TransactionType.Purchase,
            Note         = $"Mua gói Auto-Fish lần #{profile.AutoFishPurchaseCount + 1}",
            CreatedAt    = now
        });

        // ── Cập nhật profile ─────────────────────────────────────────────────
        // Kết quả luôn post vào FishingChannelId nếu đã set, fallback về channel hiện tại
        var guildConfig = await _configRepo.GetByGuildIdAsync(Context.Guild.Id);
        var fishChannel = guildConfig?.FishingChannelId ?? Context.Channel.Id;

        profile.AutoFishExpiresAt    = newExpires;
        profile.AutoFishSellAll      = true;
        profile.AutoFishPurchaseCount++;
        profile.AutoFishChannelId    = fishChannel;

        await _profileRepo.SaveChangesAsync();
        await _walletRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[AutoFish-User] {UserId} bought #{Count} for {Price} coins in guild {GuildId}",
            Context.User.Id, profile.AutoFishPurchaseCount, price, Context.Guild.Id);

        // ── Tính giá lần tiếp theo để hiển thị ──────────────────────────────
        var nextPrice = GetPrice(profile.AutoFishPurchaseCount); // count đã tăng rồi
        var expiresUnix = ((DateTimeOffset)newExpires).ToUnixTimeSeconds();

        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle(isRenew ? "🔄 Auto-Fish Gia Hạn!" : "🎣 Auto-Fish Kích Hoạt!")
            .WithDescription(
                $"Bot sẽ tự câu cho bạn mỗi **27 giây**.\n" +
                $"Cá câu được sẽ **tự bán toàn bộ** vào ví.\n\n" +
                $"⏰ Hết hạn: <t:{expiresUnix}:F>\n" +
                $"_(Còn <t:{expiresUnix}:R>)_")
            .AddField("Đã thanh toán", $"💰 **{price:N0}** coins", inline: true)
            .AddField("Số dư còn lại", $"💰 **{wallet.Coins:N0}** coins", inline: true)
            .AddField("Lần mua thứ", $"**#{profile.AutoFishPurchaseCount}**", inline: true)
            .AddField("Giá lần tiếp theo",
                profile.AutoFishPurchaseCount >= PriceTiers.Length
                    ? $"💰 **{nextPrice:N0}** coins _(giá trần)_"
                    : $"💰 **{nextPrice:N0}** coins",
                inline: false)
            .WithFooter("Dùng /fish-auto stop để dừng sớm • Không hoàn tiền khi dừng giữa chừng")
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── /fish-auto pause ─────────────────────────────────────────────────────

    [SlashCommand("pause", "Tạm dừng auto câu cá (timer vẫn chạy, không mất thời gian)")]
    public async Task PauseAsync()
    {
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        var now = DateTime.UtcNow;

        if (profile.AutoFishExpiresAt is null || profile.AutoFishExpiresAt <= now || !profile.AutoFishSellAll)
        {
            await FollowupAsync("ℹ️ Bạn không có session auto-fish nào đang chạy.", ephemeral: true);
            return;
        }

        if (profile.AutoFishPaused)
        {
            await FollowupAsync("⏸️ Session đang đã tạm dừng rồi. Dùng `/fish-auto resume` để tiếp tục.", ephemeral: true);
            return;
        }

        profile.AutoFishPaused = true;
        await _profileRepo.SaveChangesAsync();

        await FollowupAsync("⏸️ Đã tạm dừng auto câu cá. Dùng `/fish-auto resume` để tiếp tục.", ephemeral: true);
    }

    // ── /fish-auto resume ─────────────────────────────────────────────────────

    [SlashCommand("resume", "Tiếp tục auto câu cá sau khi tạm dừng")]
    public async Task ResumeAsync()
    {
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        var now = DateTime.UtcNow;

        if (profile.AutoFishExpiresAt is null || profile.AutoFishExpiresAt <= now || !profile.AutoFishSellAll)
        {
            await FollowupAsync("ℹ️ Bạn không có session auto-fish nào đang chạy.", ephemeral: true);
            return;
        }

        if (!profile.AutoFishPaused)
        {
            await FollowupAsync("▶️ Session đang hoạt động bình thường rồi!", ephemeral: true);
            return;
        }

        profile.AutoFishPaused = false;
        await _profileRepo.SaveChangesAsync();

        await FollowupAsync("▶️ Đã tiếp tục auto câu cá!", ephemeral: true);
    }

    // ── /fish-auto stop ──────────────────────────────────────────────────────

    [SlashCommand("stop", "Dừng session auto câu cá sớm (không hoàn tiền)")]
    public async Task StopAsync()
    {
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        var now = DateTime.UtcNow;

        if (profile.AutoFishExpiresAt is null || profile.AutoFishExpiresAt <= now)
        {
            await FollowupAsync("ℹ️ Bạn không có session auto-fish nào đang chạy.", ephemeral: true);
            return;
        }

        // Không cho user dừng session của admin
        if (!profile.AutoFishSellAll)
        {
            await FollowupAsync(
                "⚠️ Đây là session **Admin** — chỉ Admin mới dừng được bằng `/auto-fish stop`.",
                ephemeral: true);
            return;
        }

        profile.AutoFishExpiresAt = null;
        profile.AutoFishSellAll   = false;
        await _profileRepo.SaveChangesAsync();

        await FollowupAsync(
            "⛔ Session auto-fish đã dừng.\n⚠️ Không hoàn tiền cho thời gian còn lại.",
            ephemeral: true);
    }

    // ── /fish-auto status ────────────────────────────────────────────────────

    [SlashCommand("status", "Xem trạng thái gói auto câu cá")]
    public async Task StatusAsync()
    {
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        var now      = DateTime.UtcNow;
        var isActive = profile.AutoFishExpiresAt.HasValue
                    && profile.AutoFishExpiresAt > now
                    && profile.AutoFishSellAll;

        var nextPrice  = GetPrice(profile.AutoFishPurchaseCount);
        var purchaseNo = profile.AutoFishPurchaseCount;

        EmbedBuilder embed;

        if (isActive)
        {
            var expiresUnix = ((DateTimeOffset)profile.AutoFishExpiresAt!.Value).ToUnixTimeSeconds();
            embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("🎣 Auto-Fish Đang Chạy")
                .WithDescription($"Hết hạn: <t:{expiresUnix}:F>\n_(Còn <t:{expiresUnix}:R>)_")
                .AddField("Đã mua", $"**{purchaseNo}** lần", inline: true)
                .AddField("Giá gia hạn tiếp theo", $"💰 **{nextPrice:N0}** coins", inline: true)
                .WithFooter("Dùng /fish-auto stop để dừng • /fish-auto buy để gia hạn ngay");
        }
        else
        {
            embed = new EmbedBuilder()
                .WithColor(Color.LightGrey)
                .WithTitle("⛔ Auto-Fish Không Hoạt Động")
                .WithDescription($"Dùng `/fish-auto buy` để kích hoạt **3 ngày** auto câu.")
                .AddField("Lần mua tiếp theo", $"Lần **#{purchaseNo + 1}**", inline: true)
                .AddField("Giá", $"💰 **{nextPrice:N0}** coins", inline: true);

            // Hiện bảng giá nếu chưa mua bao giờ
            if (purchaseNo == 0)
            {
                embed.AddField("📋 Bảng giá leo thang",
                    string.Join("\n", PriceTiers.Select((p, i) =>
                        i < PriceTiers.Length - 1
                            ? $"Lần {i + 1}: **{p:N0}** coins"
                            : $"Lần {i + 1}+: **{p:N0}** coins _(giá trần)_")),
                    inline: false);
            }
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    /// <summary>Lấy giá theo số lần đã mua (index clamped vào PriceTiers).</summary>
    private static long GetPrice(int purchaseCount)
        => PriceTiers[Math.Min(purchaseCount, PriceTiers.Length - 1)];
}
