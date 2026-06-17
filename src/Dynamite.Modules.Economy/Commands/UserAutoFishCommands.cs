// src/Dynamite.Modules.Economy/Commands/UserAutoFishCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Microsoft.Extensions.Logging;

// Enum cho lựa chọn mode auto fish — Discord render thành dropdown
public enum AutoFishMode
{
    [ChoiceDisplay("🎣 Bể Thường")]
    Regular     = 0,
    [ChoiceDisplay("⭐ Pool Đặc Biệt")]
    SpecialPool = 1,
}

/// <summary>
/// /fish-auto — Gói auto câu cá subscription cho user thường.
///
/// Cơ chế:
/// - Mua trực tiếp bằng coins, KHÔNG cần item trong shop
/// - Mỗi lần mua = 5 tiếng auto câu; sau khi hết phải đợi 1 tiếng cooldown mới mua lại
/// - Giá leo thang theo số lần mua (lần 1 rẻ, càng về sau càng đắt)
/// - Bot câu mỗi 27 giây, cá câu được lưu vào túi (KHÔNG tự bán)
/// - Khi túi đầy → session tự pause, bot báo vào channel câu cá
///
/// Bảng giá (PriceTiers):
///   Lần 1:  5,000 coins   ← khuyến mãi đầu tiên
///   Lần 2:  12,000 coins
///   Lần 3:  25,000 coins
///   Lần 4:  45,000 coins
///   Lần 5+: 70,000 coins  ← giá trần
/// </summary>
[RequireContext(ContextType.Guild)]
[Group("fish-auto", "Gói auto câu cá (mua bằng coins, max 5 tiếng/lần)")]
public class UserAutoFishCommands : InteractionModuleBase<SocketInteractionContext>
{
    // ── Cấu hình ─────────────────────────────────────────────────────────────

    /// <summary>Thời hạn mỗi lần mua: 5 tiếng.</summary>
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(5);

    /// <summary>Thời gian chờ bắt buộc sau khi session kết thúc trước khi mua lại.</summary>
    private static readonly TimeSpan BuyCooldown = TimeSpan.FromHours(1);

    /// <summary>
    /// Bảng giá theo lần mua (index = purchaseCount, index cuối là giá trần).
    /// Lần mua 5 trở đi đều trả giá tiers[^1].
    /// </summary>
    private static readonly long[] PriceTiers = [5_000, 12_000, 25_000, 45_000, 70_000];

    // ─────────────────────────────────────────────────────────────────────────

    private readonly IUserProfileRepository  _profileRepo;
    private readonly IWalletRepository       _walletRepo;
    private readonly IGuildConfigRepository  _configRepo;
    private readonly ISpecialPoolRepository  _poolRepo;
    private readonly IShopRepository         _shopRepo;
    private readonly ILogger<UserAutoFishCommands> _logger;

    public UserAutoFishCommands(
        IUserProfileRepository  profileRepo,
        IWalletRepository       walletRepo,
        IGuildConfigRepository  configRepo,
        ISpecialPoolRepository  poolRepo,
        IShopRepository         shopRepo,
        ILogger<UserAutoFishCommands> logger)
    {
        _profileRepo = profileRepo;
        _walletRepo  = walletRepo;
        _configRepo  = configRepo;
        _poolRepo    = poolRepo;
        _shopRepo    = shopRepo;
        _logger      = logger;
    }

    // ── /fish-auto buy ───────────────────────────────────────────────────────

    [SlashCommand("buy", "Mua / gia hạn gói auto câu cá 5 tiếng")]
    public async Task BuyAsync()
    {
        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);
        var wallet = await _walletRepo.GetOrCreateAsync(
            Context.Guild.Id, Context.User.Id);

        var now = DateTime.UtcNow;

        // ── Kiểm tra cooldown 1 tiếng sau khi session kết thúc ──────────────
        if (profile.AutoFishExpiresAt.HasValue
            && profile.AutoFishExpiresAt <= now
            && profile.AutoFishExpiresAt > now - BuyCooldown)
        {
            var cooldownEnds = profile.AutoFishExpiresAt.Value + BuyCooldown;
            var cooldownUnix = ((DateTimeOffset)cooldownEnds).ToUnixTimeSeconds();
            await FollowupAsync(
                $"⏳ **Cần đợi 1 tiếng sau khi session kết thúc!**\n" +
                $"Có thể mua lại <t:{cooldownUnix}:R> (<t:{cooldownUnix}:T>).",
                ephemeral: true);
            return;
        }

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

        var cooldownEndsUnix = ((DateTimeOffset)newExpires.Add(BuyCooldown)).ToUnixTimeSeconds();

        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle(isRenew ? "🔄 Auto-Fish Gia Hạn!" : "🎣 Auto-Fish Kích Hoạt!")
            .WithDescription(
                $"Bot sẽ tự câu cho bạn mỗi **27 giây**.\n" +
                $"Cá câu được sẽ **lưu vào túi cá** (không tự bán).\n" +
                $"Khi túi đầy bot sẽ **tạm dừng** và báo vào kênh câu cá.\n\n" +
                $"⏰ Hết hạn: <t:{expiresUnix}:F> _(còn <t:{expiresUnix}:R>)_\n" +
                $"🔒 Mua lại được sau: <t:{cooldownEndsUnix}:T> _(1 tiếng sau khi hết)_")
            .AddField("Đã thanh toán", $"💰 **{price:N0}** coins", inline: true)
            .AddField("Số dư còn lại", $"💰 **{wallet.Coins:N0}** coins", inline: true)
            .AddField("Lần mua thứ", $"**#{profile.AutoFishPurchaseCount}**", inline: true)
            .AddField("Giá lần tiếp theo",
                profile.AutoFishPurchaseCount >= PriceTiers.Length
                    ? $"💰 **{nextPrice:N0}** coins _(giá trần)_"
                    : $"💰 **{nextPrice:N0}** coins",
                inline: false)
            .WithFooter("Dùng /fish-auto stop để dừng sớm • Không hoàn tiền • Dừng sớm vẫn tính cooldown")
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

        // Đặt ExpiresAt = now thay vì null để cooldown 1h được tính từ thời điểm dừng
        profile.AutoFishExpiresAt = DateTime.UtcNow;
        profile.AutoFishSellAll   = false;
        profile.AutoFishPaused    = false;
        await _profileRepo.SaveChangesAsync();

        var cooldownUnix = ((DateTimeOffset)DateTime.UtcNow.Add(BuyCooldown)).ToUnixTimeSeconds();
        await FollowupAsync(
            $"⛔ Session auto-fish đã dừng.\n" +
            $"⚠️ Không hoàn tiền cho thời gian còn lại.\n" +
            $"🔒 Có thể mua lại <t:{cooldownUnix}:R> (<t:{cooldownUnix}:T>).",
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
        var inCooldown = !isActive
                    && profile.AutoFishExpiresAt.HasValue
                    && profile.AutoFishExpiresAt <= now
                    && profile.AutoFishExpiresAt > now - BuyCooldown;

        var nextPrice  = GetPrice(profile.AutoFishPurchaseCount);
        var purchaseNo = profile.AutoFishPurchaseCount;

        EmbedBuilder embed;

        if (isActive)
        {
            var expiresUnix      = ((DateTimeOffset)profile.AutoFishExpiresAt!.Value).ToUnixTimeSeconds();
            var cooldownEndsUnix = ((DateTimeOffset)profile.AutoFishExpiresAt.Value.Add(BuyCooldown)).ToUnixTimeSeconds();
            var pausedNote       = profile.AutoFishPaused ? "\n⏸️ **Đang tạm dừng** — dùng `/fish-auto resume` để tiếp tục." : "";
            embed = new EmbedBuilder()
                .WithColor(profile.AutoFishPaused ? Color.Orange : Color.Blue)
                .WithTitle(profile.AutoFishPaused ? "⏸️ Auto-Fish Đang Tạm Dừng" : "🎣 Auto-Fish Đang Chạy")
                .WithDescription(
                    $"⏰ Hết hạn: <t:{expiresUnix}:F> _(còn <t:{expiresUnix}:R>)_\n" +
                    $"🔒 Mua lại được sau: <t:{cooldownEndsUnix}:T>{pausedNote}")
                .AddField("Đã mua", $"**{purchaseNo}** lần", inline: true)
                .AddField("Giá gia hạn tiếp theo", $"💰 **{nextPrice:N0}** coins", inline: true)
                .WithFooter("Dùng /fish-auto stop để dừng • /fish-auto buy để gia hạn ngay");
        }
        else if (inCooldown)
        {
            var cooldownEnds = profile.AutoFishExpiresAt!.Value + BuyCooldown;
            var cooldownUnix = ((DateTimeOffset)cooldownEnds).ToUnixTimeSeconds();
            embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("⏳ Đang Cooldown — Chưa Thể Mua Lại")
                .WithDescription(
                    $"Session vừa kết thúc. Phải đợi **1 tiếng** trước khi mua lại.\n\n" +
                    $"🔓 Mua lại được: <t:{cooldownUnix}:R> (<t:{cooldownUnix}:T>)\n\n" +
                    $"💡 Tranh thủ dùng `/bag sell-all` để chuẩn bị coins!")
                .AddField("Lần mua tiếp theo", $"Lần **#{purchaseNo + 1}**", inline: true)
                .AddField("Giá", $"💰 **{nextPrice:N0}** coins", inline: true);
        }
        else
        {
            embed = new EmbedBuilder()
                .WithColor(Color.LightGrey)
                .WithTitle("⛔ Auto-Fish Không Hoạt Động")
                .WithDescription($"Dùng `/fish-auto buy` để kích hoạt **5 tiếng** auto câu.\nSau khi hết cần đợi **1 tiếng cooldown** trước khi mua lại.")
                .AddField("Lần mua tiếp theo", $"Lần **#{purchaseNo + 1}**", inline: true)
                .AddField("Giá", $"💰 **{nextPrice:N0}** coins", inline: true);

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

    // ── /fish-auto set-mode ──────────────────────────────────────────────────

    [SlashCommand("set-mode", "Chọn chế độ auto câu: bể thường hoặc pool đặc biệt")]
    public async Task SetModeAsync(
        [Summary("mode", "Chế độ auto câu")] AutoFishMode mode,
        [Summary("pool_id", "ID của pool đặc biệt (lấy từ /fishing pools, bắt buộc khi chọn SpecialPool)")]
        string? poolId = null)
    {
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);
        var now = DateTime.UtcNow;

        // ── Kiểm tra session đang active ─────────────────────────────────────
        var sessionActive = profile.AutoFishExpiresAt.HasValue
                         && profile.AutoFishExpiresAt > now
                         && profile.AutoFishSellAll;

        if (!sessionActive)
        {
            await FollowupAsync(
                "❌ Bạn không có session auto-fish đang chạy.\n" +
                "Dùng `/fish-auto buy` để kích hoạt trước.",
                ephemeral: true);
            return;
        }

        // ── Mode: Regular ────────────────────────────────────────────────────
        if (mode == AutoFishMode.Regular)
        {
            profile.AutoFishSpecialPoolId        = null;
            profile.AutoFishSpecialPoolExpiresAt = null;
            await _profileRepo.SaveChangesAsync();

            await FollowupAsync(
                "🎣 Đã chuyển về chế độ **Bể Thường**.\n" +
                "Bot sẽ câu bể thường từ tick tiếp theo.",
                ephemeral: true);
            return;
        }

        // ── Mode: SpecialPool — validate ─────────────────────────────────────
        if (string.IsNullOrWhiteSpace(poolId) || !Guid.TryParse(poolId, out var parsedPoolId))
        {
            await FollowupAsync(
                "❌ Cần cung cấp **pool_id** hợp lệ khi chọn Pool Đặc Biệt.\n" +
                "Dùng `/fishing pools` để xem danh sách pool và ID của chúng.",
                ephemeral: true);
            return;
        }

        // ── Check pool tồn tại + active ──────────────────────────────────────
        var pool = await _poolRepo.GetByIdAsync(parsedPoolId);
        if (pool is null || !pool.IsActive || pool.GuildId != Context.Guild.Id)
        {
            await FollowupAsync(
                "❌ Pool này không tồn tại hoặc không còn hoạt động.\n" +
                "Dùng `/fishing pools` để xem các pool đang active.",
                ephemeral: true);
            return;
        }

        // ── Check fishing level ───────────────────────────────────────────────
        if (profile.FishingLevel < pool.MinLevel)
        {
            await FollowupAsync(
                $"❌ Cần **Fishing Level {pool.MinLevel}** để câu tại **{pool.PoolName}**.\n" +
                $"Level hiện tại của bạn: **{profile.FishingLevel}**.",
                ephemeral: true);
            return;
        }

        // ── Check có vé không và tiêu 1 vé ──────────────────────────────────
        var wallet    = await _walletRepo.GetOrCreateAsync(Context.Guild.Id, Context.User.Id);
        var inventory = await _shopRepo.GetUserInventoryAsync(wallet.Id);
        var tickets   = inventory.FirstOrDefault(i => i.Item.Type == ItemType.PoolTicket && i.Quantity > 0);

        if (tickets is null)
        {
            await FollowupAsync(
                "❌ Bạn không có **Vé Pool Đặc Biệt** nào.\n" +
                "Mua vé tại `/shop buy Vé Pool Đặc Biệt` — 1 vé = **2 tiếng** câu pool đặc biệt.",
                ephemeral: true);
            return;
        }

        // Tiêu 1 vé ngay khi kích hoạt
        tickets.Quantity--;
        if (tickets.Quantity <= 0)
            await _shopRepo.RemoveUserInventoryAsync(tickets);
        await _shopRepo.SaveChangesAsync();

        // ── Tất cả điều kiện đã qua → set mode ───────────────────────────────
        var ticketExpiry     = now.AddHours(2);
        var ticketExpiryUnix = ((DateTimeOffset)ticketExpiry).ToUnixTimeSeconds();

        profile.AutoFishSpecialPoolId        = parsedPoolId;
        profile.AutoFishSpecialPoolExpiresAt = ticketExpiry;
        await _profileRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[AutoFish-User] {UserId} activated special pool mode → pool {PoolId} until {Expiry} in guild {GuildId}",
            Context.User.Id, parsedPoolId, ticketExpiry, Context.Guild.Id);

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0xF39C12))
                .WithTitle("⭐ Auto-Fish: Pool Đặc Biệt")
                .WithDescription(
                    $"Bot sẽ tự câu tại **{pool.PoolName}** từ tick tiếp theo.\n\n" +
                    $"🎟️ Đã sử dụng **1 Vé Pool Đặc Biệt** — có hiệu lực **2 tiếng**.\n" +
                    $"⏰ Hết hạn vé: <t:{ticketExpiryUnix}:F> _(còn <t:{ticketExpiryUnix}:R>)_\n\n" +
                    $"Sau khi hết hạn, bot tự chuyển về **Bể Thường**.\n" +
                    $"Dùng `/fish-auto set-mode Regular` để chuyển về sớm hơn.")
                .AddField("Pool", pool.PoolName, inline: true)
                .AddField("Level yêu cầu", $"Level {pool.MinLevel}+", inline: true)
                .AddField("Vé còn lại", $"🎟️ {tickets.Quantity}", inline: true)
                .WithFooter("Auto-Fish Pool Mode • 1 vé = 2 tiếng • Mua thêm tại /shop buy")
                .Build(),
            ephemeral: true);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    /// <summary>Lấy giá theo số lần đã mua (index clamped vào PriceTiers).</summary>
    private static long GetPrice(int purchaseCount)
        => PriceTiers[Math.Min(purchaseCount, PriceTiers.Length - 1)];
}
