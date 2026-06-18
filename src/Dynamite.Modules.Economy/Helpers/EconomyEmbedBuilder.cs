// src/Dynamite.Modules.Economy/Helpers/EconomyEmbedBuilder.cs
namespace Dynamite.Modules.Economy.Helpers;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Modules.Economy.Services;

public static class EconomyEmbedBuilder
{
    // ── Wallet / Balance ──────────────────────────────────────────────────────

    public static Embed BuildBalanceEmbed(UserWallet wallet, string username)
        => new EmbedBuilder()
            .WithTitle($"🪙 Ví của {username}")
            .WithDescription($"**Số dư:** {wallet.Coins:N0} coins")
            .WithColor(new Color(0xF5A623))
            .AddField("📅 Streak điểm danh", $"{wallet.DailyStreak} ngày", inline: true)
            .AddField("📆 Điểm danh gần nhất",
                wallet.LastDaily.HasValue
                    ? $"<t:{new DateTimeOffset(wallet.LastDaily.Value).ToUnixTimeSeconds()}:R>"
                    : "Chưa bao giờ",
                inline: true)
            .Build();

    // ── Daily ─────────────────────────────────────────────────────────────────

    public static Embed BuildDailyEmbed(long coinsEarned, long total, int streak)
    {
        var streakText = streak > 1 ? $" (🔥 streak {streak} ngày!)" : "";
        return new EmbedBuilder()
            .WithTitle("📅 Điểm Danh Hằng Ngày")
            .WithDescription(
                $"Bạn nhận được **{coinsEarned:N0} coins**{streakText}!\n\n" +
                $"💰 Số dư: **{total:N0} coins**")
            .WithColor(new Color(0x57F287))
            .WithFooter("Quay lại ngày mai để giữ streak!")
            .Build();
    }

    // ── Fishing ───────────────────────────────────────────────────────────────

    public static Embed BuildFishEmbed(FishResult result)
    {
        var c        = result.Catch;
        var weather  = WeatherService.GetWeatherEmoji(result.Weather);
        var rodInfo  = result.RodName != null ? $"🎣 Cần: {result.RodName}\n" : "";
        var xpInfo   = result.FishingXpGained > 0 ? $"✨ +{result.FishingXpGained} Fishing XP\n" : "";
        var pondInfo = $"🪣 Bể còn: **{result.PondRemaining:N0}** con";

        var bagStatus = result.SavedToBag
            ? result.BagFreeSlots <= 3
                ? $"\n📦 Túi còn **{result.BagFreeSlots}** slot — bán sớm nhé!"
                : ""
            : "\n\n⚠️ **Túi đầy!** Cá không được lưu — dùng `/bag sell-all` rồi câu lại.";

        var levelUpText = result.FishingLevelUp is { LeveledUp: true }
            ? $"\n\n🎉 **Fishing Level Up! → Lv.{result.FishingLevelUp.NewLevel}**" +
              (result.FishingLevelUp.RoleAwarded.HasValue
                  ? $"\n🎖️ Nhận role: <@&{result.FishingLevelUp.RoleAwarded.Value}>!"
                  : "")
            : "";

        var achieveText = result.NewAchievements.Count > 0
            ? "\n\n🏆 **Thành tựu mới:**\n" +
              string.Join("\n", result.NewAchievements.Select(a =>
                  $"{a.Title} — +{a.CoinReward:N0} coins"))
            : "";

        var isTrash    = c.Rarity == "Trash";
        var chestLabel = c.IsChest ? "Hòm Báu" : isTrash ? "Rác" : "Cá";
        var catchVerb  = isTrash ? "Vớt được" : "Bắt được";
        var coinLine   = isTrash
            ? "💸 Giá trị: **0 coins** _(rác rưởi thôi...)_\n\n"
            : $"💰 Giá trị: **~{c.Coins:N0} coins** _(bán cá để nhận)_\n\n";

        var rodDurText = result.RodJustBroke
            ? "\n\n💔 **Cần câu vừa gãy!** Dùng `/shop repair-rod` để sửa trước khi câu tiếp."
            : result.RodDurabilityLeft.HasValue && result.RodDurabilityLeft <= 20
                ? $"\n\n⚠️ Cần câu còn **{result.RodDurabilityLeft}** lần câu — sắp gãy!"
                : "";

        return new EmbedBuilder()
            .WithTitle($"{c.Emoji} {catchVerb} {chestLabel}: {c.Name}!")
            .WithDescription(
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                coinLine +
                $"{rodInfo}{xpInfo}{pondInfo} {weather}" +
                bagStatus + levelUpText + achieveText + rodDurText)
            .WithColor(RarityColor(c.Rarity))
            .Build();
    }

    /// <summary>
    /// Embed cho auto-fish (user mode) — có username và countdown.
    /// </summary>
    public static Embed BuildAutoFishEmbed(FishResult result, DateTime expiresAt, string username)
    {
        var c        = result.Catch;
        var isTrash  = c.Rarity == "Trash";
        var weather  = WeatherService.GetWeatherEmoji(result.Weather);
        var rodInfo  = result.RodName != null ? $"🎣 Cần: {result.RodName}\n" : "";
        var xpInfo   = result.FishingXpGained > 0 ? $"✨ +{result.FishingXpGained} Fishing XP\n" : "";
        var pondInfo = $"🪣 Bể còn: **{result.PondRemaining:N0}** con";
        var coinLine = isTrash
            ? "💸 Giá trị: **0 coins** _(rác rưởi thôi...)_\n\n"
            : $"💰 Giá trị: **~{c.Coins:N0} coins** _(bán cá để nhận)_\n\n";

        var levelUpText = result.FishingLevelUp is { LeveledUp: true }
            ? $"\n\n🎉 **Fishing Level Up! → Lv.{result.FishingLevelUp.NewLevel}**" +
              (result.FishingLevelUp.RoleAwarded.HasValue
                  ? $"\n🎖️ Nhận role: <@&{result.FishingLevelUp.RoleAwarded.Value}>!"
                  : "")
            : "";

        var achieveText = result.NewAchievements.Count > 0
            ? "\n\n🏆 **Thành tựu mới:**\n" +
              string.Join("\n", result.NewAchievements.Select(a =>
                  $"{a.Title} — +{a.CoinReward:N0} coins"))
            : "";

        var rodDurText = result.RodJustBroke
            ? "\n\n💔 **Cần câu vừa gãy!** Auto-fish đã tạm dừng — dùng `/shop repair-rod` để sửa."
            : result.RodDurabilityLeft.HasValue && result.RodDurabilityLeft <= 20
                ? $"\n\n⚠️ Cần câu còn **{result.RodDurabilityLeft}** lần câu — sắp gãy!"
                : "";

        var remaining    = expiresAt - DateTime.UtcNow;
        var countdownStr = remaining.TotalSeconds > 0 ? FormatRemaining(remaining) : "Hết hạn";
        var chestLabel   = c.IsChest ? "Hòm Báu" : isTrash ? "Rác" : "Cá";
        var catchVerb    = isTrash ? "Vớt được" : "Bắt được";

        return new EmbedBuilder()
            .WithTitle($"🤖 [Auto] {username} {c.Emoji} {catchVerb} {chestLabel}: {c.Name}!")
            .WithDescription(
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                coinLine +
                $"{rodInfo}{xpInfo}{pondInfo} {weather}" +
                levelUpText + achieveText + rodDurText)
            .WithColor(RarityColor(c.Rarity))
            .WithFooter($"⏱️ Auto-fish còn lại: {countdownStr}")
            .Build();
    }

    /// <summary>
    /// Embed cho auto-fish admin mode — có username, KHÔNG có countdown.
    /// </summary>
    public static Embed BuildAdminAutoFishEmbed(FishResult result, string username)
    {
        var c        = result.Catch;
        var isTrash  = c.Rarity == "Trash";
        var weather  = WeatherService.GetWeatherEmoji(result.Weather);
        var rodInfo  = result.RodName != null ? $"🎣 Cần: {result.RodName}\n" : "";
        var xpInfo   = result.FishingXpGained > 0 ? $"✨ +{result.FishingXpGained} Fishing XP\n" : "";
        var pondInfo = $"🪣 Bể còn: **{result.PondRemaining:N0}** con";
        var coinLine = isTrash
            ? "💸 Giá trị: **0 coins** _(rác rưởi thôi...)_\n\n"
            : $"💰 Giá trị: **~{c.Coins:N0} coins** _(bán cá để nhận)_\n\n";

        var levelUpText = result.FishingLevelUp is { LeveledUp: true }
            ? $"\n\n🎉 **Fishing Level Up! → Lv.{result.FishingLevelUp.NewLevel}**" +
              (result.FishingLevelUp.RoleAwarded.HasValue
                  ? $"\n🎖️ Nhận role: <@&{result.FishingLevelUp.RoleAwarded.Value}>!"
                  : "")
            : "";

        var achieveText = result.NewAchievements.Count > 0
            ? "\n\n🏆 **Thành tựu mới:**\n" +
              string.Join("\n", result.NewAchievements.Select(a =>
                  $"{a.Title} — +{a.CoinReward:N0} coins"))
            : "";

        var rodDurText = result.RodJustBroke
            ? "\n\n💔 **Cần câu vừa gãy!** Auto-fish đã tạm dừng — dùng `/shop repair-rod` để sửa."
            : result.RodDurabilityLeft.HasValue && result.RodDurabilityLeft <= 20
                ? $"\n\n⚠️ Cần câu còn **{result.RodDurabilityLeft}** lần câu — sắp gãy!"
                : "";

        var chestLabel = c.IsChest ? "Hòm Báu" : isTrash ? "Rác" : "Cá";
        var catchVerb  = isTrash ? "Vớt được" : "Bắt được";

        return new EmbedBuilder()
            .WithTitle($"🛠️ [Admin Auto] {username} {c.Emoji} {catchVerb} {chestLabel}: {c.Name}!")
            .WithDescription(
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                coinLine +
                $"{rodInfo}{xpInfo}{pondInfo} {weather}" +
                levelUpText + achieveText + rodDurText)
            .WithColor(RarityColor(c.Rarity))
            .WithFooter("🛠️ Admin Auto-Fish")
            .Build();
    }

    /// <summary>
    /// Embed cho auto-fish user mode — Special Pool variant.
    /// Hiển thị tên pool, countdown, pearl cap warning nếu có.
    /// </summary>
    public static Embed BuildAutoSpecialFishEmbed(
        SpecialFishResult result, DateTime expiresAt, string username)
    {
        var c = result.Catch;

        var pearlCapMsg = result.PearlCapReached
            ? "\n⚠️ _Ngọc quý đã đạt giới hạn tuần — nhận cá thay thế._"
            : "";

        var levelUpText = result.FishingLevelUp is { LeveledUp: true }
            ? $"\n\n🎉 **Fishing Level Up! → Lv.{result.FishingLevelUp.NewLevel}**" +
              (result.FishingLevelUp.RoleAwarded.HasValue
                  ? $"\n🎖️ Role mới: <@&{result.FishingLevelUp.RoleAwarded.Value}>!"
                  : "")
            : "";

        var isTrash      = c.Rarity == "Trash";
        var catchVerb    = isTrash ? "Vớt được" : "Bắt được";
        var coinLine     = isTrash
            ? "💸 Giá trị: **0 coins** _(cần câu yếu — nâng cấp để câu pool hiệu quả hơn!)_\n"
            : $"💰 Giá trị: **~{c.Coins:N0} coins** _(bán cá để nhận)_\n";
        var xpLine       = result.FishingXpGained > 0 ? $"✨ +{result.FishingXpGained} Fishing XP\n" : "";

        var remaining    = expiresAt - DateTime.UtcNow;
        var countdownStr = remaining.TotalSeconds > 0 ? FormatRemaining(remaining) : "Hết hạn";

        return new EmbedBuilder()
            .WithTitle($"⭐ [Auto Pool] {username} {c.Emoji} {catchVerb}: {c.Name}!")
            .WithDescription(
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                coinLine +
                xpLine +
                $"🪣 Pool còn: **{result.PondRemaining:N0}** con" +
                pearlCapMsg + levelUpText)
            .WithColor(isTrash ? new Color(0x636e72u) : new Color(c.IsPearl ? 0xFFD700u : 0xF39C12u))
            .WithFooter($"⏱️ Auto-fish còn lại: {countdownStr} • 🎟️ -1 vé")
            .Build();
    }

    private static string FormatRemaining(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}n {ts.Hours}g {ts.Minutes}p";
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}g {ts.Minutes}p";
        return $"{ts.Minutes}p {ts.Seconds}s";
    }

    public static Embed BuildPondStatusEmbed(PondStatus status)
    {
        var weatherEmoji = WeatherService.GetWeatherEmoji(status.Weather);
        var weatherExp   = new DateTimeOffset(status.WeatherExpiresAt).ToUnixTimeSeconds();

        string pondDesc;
        if (status.IsEmpty)
        {
            var resetTs = new DateTimeOffset(status.ResetAvailableAt!.Value).ToUnixTimeSeconds();
            pondDesc = $"🪣 Bể **đang trống** — cá mới về <t:{resetTs}:R>";
        }
        else
        {
            var pct = (double)status.CurrentFish / status.MaxFish * 100;
            var bar = BuildProgressBar(pct, 10);
            pondDesc = $"🐟 {bar} **{status.CurrentFish:N0}/{status.MaxFish:N0}** con";
        }

        // Weather effect description
        var (rareMod, legendaryMod, _, missMod, coinMod) = WeatherService.GetModifiers(status.Weather);
        var effectLines = new System.Text.StringBuilder();
        if (missMod < 0)
            effectLines.Append($"🎣 Cá cắn câu nhiều hơn **{Math.Abs(missMod * 100):0}%** — sản lượng cao\n");
        else if (missMod > 0)
            effectLines.Append($"🌊 Cá khó bắt hơn **{missMod * 100:0}%** — sản lượng thấp\n");
        if (rareMod > 0)
            effectLines.Append($"🐡 Tỉ lệ Hiếm **+{rareMod * 100:0}%**\n");
        if (legendaryMod > 0)
            effectLines.Append($"🦈 Tỉ lệ Huyền Thoại **+{legendaryMod * 100:0}%**\n");
        if (coinMod > 1.0)
            effectLines.Append($"💰 Giá trị cá **×{coinMod:0.##}** (thưởng nguy hiểm)\n");

        var effectStr = effectLines.Length > 0
            ? $"\n**Hiệu ứng thời tiết:**\n{effectLines.ToString().TrimEnd()}"
            : "\n*Thời tiết bình thường — không có bonus.*";

        var color = status.Weather switch
        {
            PondWeather.Rainy  => new Color(0x3498DB),
            PondWeather.Stormy => new Color(0x9B59B6),
            _                  => status.IsEmpty ? new Color(0xED4245) : new Color(0x1abc9c)
        };

        return new EmbedBuilder()
            .WithTitle("🌊 Trạng Thái Bể Cá")
            .WithDescription(
                $"{pondDesc}\n\n" +
                $"**Thời tiết:** {weatherEmoji} {status.Weather}\n" +
                $"Thay đổi: <t:{weatherExp}:R>" +
                effectStr)
            .WithColor(color)
            .Build();
    }

    // ── Leaderboard ───────────────────────────────────────────────────────────

    public static Embed BuildLeaderboardEmbed(
        List<(int rank, ulong userId, long coins)> entries, ulong guildId)
    {
        var desc = entries.Count == 0
            ? "Chưa có dữ liệu."
            : string.Join("\n", entries.Select(e =>
                $"{RankEmoji(e.rank)} <@{e.userId}> — **{e.coins:N0}** coins"));

        return new EmbedBuilder()
            .WithTitle("🏆 Bảng Xếp Hạng Coins")
            .WithDescription(desc)
            .WithColor(new Color(0xF5A623))
            .Build();
    }

    // ── Shop ──────────────────────────────────────────────────────────────────

    public static Embed BuildShopEmbed(List<InventoryItem> items, long bagUpgradePrice = 0)
    {
        var desc = items.Count == 0
            ? "Cửa hàng đang trống."
            : string.Join("\n\n", items.Select(i => BuildShopItemLine(i, bagUpgradePrice)));

        return new EmbedBuilder()
            .WithTitle("🛒 Cửa Hàng")
            .WithDescription(desc)
            .WithColor(new Color(0x5865F2))
            .WithFooter("Dùng /shop buy <tên vật phẩm> để mua")
            .Build();
    }

    private static string BuildShopItemLine(InventoryItem i, long bagUpgradePrice)
    {
        // BagUpgrade dùng giá động theo túi hiện tại của user
        var displayPrice = (i.Type == ItemType.BagUpgrade && bagUpgradePrice > 0)
            ? bagUpgradePrice
            : i.Price;

        var priceText = displayPrice == 0 ? "Đã đầy" : $"{displayPrice:N0} coins";
        var line = $"{i.Emoji} **{i.Name}** — {priceText}\n_{i.Description ?? "Không có mô tả."}_";

        return i.Type switch
        {
            ItemType.FishingRod  => line + $"\n🎣 Cooldown: {i.CooldownSeconds}s | Nhân: ×{i.DropMultiplier:F1}",
            ItemType.Bait        => line + $"\n🪱 +10% Rare | {i.UsageCount} lần dùng",
            ItemType.AutoFish    => line + $"\n🤖 Auto câu {i.DurationMinutes} phút",
            ItemType.WeatherItem => line + $"\n☔ Force Rainy {i.DurationMinutes} phút",
            ItemType.PoolTicket  => line + $"\n🎟️ 2 tiếng câu pool đặc biệt | Yêu cầu Level 20+",
            ItemType.BagUpgrade  => line + $"\n🎒 +10 slot | Túi đầy: hiển thị Đã đầy",
            _                    => line
        };
    }

    public static Embed BuildInventoryEmbed(List<UserInventory> items, string username)
    {
        if (items.Count == 0)
            return new EmbedBuilder()
                .WithTitle($"🎒 Kho Đồ của {username}")
                .WithDescription("Kho đồ đang trống.")
                .WithColor(new Color(0x5865F2))
                .Build();

        var lines = items.Select(i =>
        {
            var base_ = $"{i.Item.Emoji} **{i.Item.Name}**";

            // Rod: hiển thị durability bar
            if (i.Item.Type == ItemType.FishingRod && i.RodDurability.HasValue && i.Item.MaxDurability.HasValue)
            {
                var dur    = i.RodDurability.Value;
                var maxDur = i.Item.MaxDurability.Value;
                var pct    = (double)dur / maxDur * 100;
                var bar    = BuildProgressBar(pct, 6);
                var status = dur == 0 ? " 💔 **GÃY**" : dur <= maxDur * 0.2 ? " ⚠️" : "";
                return $"{base_}{status}\n  └ Độ bền: {bar} {dur}/{maxDur}";
            }

            // Stackable items: show quantity
            if (i.Quantity > 1)
                return $"{base_} ×{i.Quantity}";

            return base_;
        });

        return new EmbedBuilder()
            .WithTitle($"🎒 Kho Đồ của {username}")
            .WithDescription(string.Join("\n", lines))
            .WithColor(new Color(0x5865F2))
            .WithFooter("Dùng /shop repair-rod để sửa cần câu bị mòn")
            .Build();
    }

    // ── Profile (XP + Level) ──────────────────────────────────────────────────

    public static Embed BuildProfileEmbed(
        string username,
        int serverLevel, long serverXp,
        int fishingLevel, long fishingXp,
        int totalCaught, int dailyStreak)
    {
        var serverXpNeeded  = XpService.XpForNextLevel(serverLevel + 1);
        var fishingXpNeeded = XpService.XpForNextLevel(fishingLevel + 1);
        var serverBar       = BuildProgressBar((double)serverXp / serverXpNeeded * 100, 8);
        var fishingBar      = BuildProgressBar((double)fishingXp / fishingXpNeeded * 100, 8);

        return new EmbedBuilder()
            .WithTitle($"📊 Hồ Sơ của {username}")
            .WithColor(new Color(0x9b59b6))
            .AddField("🌐 Server Level",
                $"Lv.**{serverLevel}** {serverBar} {serverXp:N0}/{serverXpNeeded:N0} XP",
                inline: false)
            .AddField("🎣 Fishing Level",
                $"Lv.**{fishingLevel}** {fishingBar} {fishingXp:N0}/{fishingXpNeeded:N0} XP",
                inline: false)
            .AddField("🐟 Tổng cá đã câu", $"{totalCaught:N0}", inline: true)
            .AddField("🔥 Daily Streak",   $"{dailyStreak} ngày",    inline: true)
            .Build();
    }

    // ── Fish Bag ──────────────────────────────────────────────────────────────

    public static Embed BuildBagEmbed(UserFishBag bag, string username)
    {
        if (bag.Fish.Count == 0)
        {
            return new EmbedBuilder()
                .WithTitle($"🎒 Túi Cá của {username}")
                .WithDescription($"Túi đang trống (**0/{bag.BagCapacity}** slot).")
                .WithColor(new Color(0x607d8b))
                .WithFooter("Dùng /fishing cast để câu cá!")
                .Build();
        }

        var grouped = bag.Fish
            .GroupBy(f => f.Rarity)
            .OrderBy(g => RarityOrder(g.Key));

        var lines = grouped.Select(g =>
        {
            var totalCoins = g.Sum(f => f.CoinValue);
            return $"{RarityEmoji(g.Key)} **{RarityVi(g.Key)}** x{g.Count()} — {totalCoins:N0} coins";
        });

        var pct = (double)bag.Fish.Count / bag.BagCapacity * 100;
        var bar = BuildProgressBar(pct, 10);

        return new EmbedBuilder()
            .WithTitle($"🎒 Túi Cá của {username}")
            .WithDescription(
                $"**{bag.Fish.Count}/{bag.BagCapacity}** slot {bar}\n\n" +
                string.Join("\n", lines) +
                $"\n\n💰 Tổng giá trị: **{bag.Fish.Sum(f => f.CoinValue):N0}** coins")
            .WithColor(new Color(0x1abc9c))
            .WithFooter("Dùng /bag sell all hoặc /bag sell <độ hiếm>")
            .Build();
    }

    public static Embed BuildBagSellResultEmbed(BagSellResult result, long walletTotal)
    {
        return new EmbedBuilder()
            .WithTitle("💰 Đã Bán Cá!")
            .WithDescription(
                $"Bán được **{result.FishSold}** con cá\n" +
                $"Nhận: **+{result.CoinsEarned:N0}** coins\n\n" +
                $"💼 Còn lại trong túi: **{result.RemainingFish}** con\n" +
                $"🪙 Số dư: **{walletTotal:N0}** coins")
            .WithColor(new Color(0xF5A623))
            .Build();
    }

    // ── Special Pool ──────────────────────────────────────────────────────────

    public static Embed BuildSpecialPoolListEmbed(List<SpecialPool> pools)
    {
        if (pools.Count == 0)
        {
            return new EmbedBuilder()
                .WithTitle("🌊 Pool Đặc Biệt")
                .WithDescription("Hiện không có pool đặc biệt nào đang hoạt động.\nQuay lại sau nhé!")
                .WithColor(new Color(0x607d8b))
                .Build();
        }

        var lines = pools.Select(p =>
        {
            var expTs = new DateTimeOffset(p.ExpiresAt).ToUnixTimeSeconds();
            return $"🎣 **{p.PoolName}** — `{p.DropTable}`\n" +
                   $"   🐟 {p.RemainingFish:N0}/{p.Capacity:N0} | ⏰ <t:{expTs}:R> | 🔒 Lv.{p.MinLevel}+\n" +
                   $"   ID: `{p.Id}`";
        });

        return new EmbedBuilder()
            .WithTitle("🌊 Pool Đặc Biệt Đang Hoạt Động")
            .WithDescription(string.Join("\n\n", lines))
            .WithColor(new Color(0x3498db))
            .WithFooter("Dùng /fishing pool cast <ID> để câu")
            .Build();
    }

    public static Embed BuildSpecialFishEmbed(SpecialFishResult result, string poolName)
    {
        var c = result.Catch;

        var pearlCapMsg = result.PearlCapReached
            ? "\n⚠️ _Ngọc quý đã đạt giới hạn tuần — nhận cá thay thế._"
            : "";

        var levelUpText = result.FishingLevelUp is { LeveledUp: true }
            ? $"\n\n🎉 **Fishing Level Up! → Lv.{result.FishingLevelUp.NewLevel}**" +
              (result.FishingLevelUp.RoleAwarded.HasValue
                  ? $"\n🎖️ Role mới: <@&{result.FishingLevelUp.RoleAwarded.Value}>!"
                  : "")
            : "";

        var bagStatus = result.SavedToBag
            ? result.BagFreeSlots <= 3
                ? $"\n📦 Túi còn **{result.BagFreeSlots}** slot — bán sớm nhé!"
                : $"\n📦 Đã lưu vào túi ({result.BagFreeSlots} slot còn lại)"
            : "\n\n⚠️ **Túi đầy!** Cá không được lưu — dùng `/bag sell-all` rồi câu lại.";

        return new EmbedBuilder()
            .WithTitle($"{c.Emoji} [Pool Đặc Biệt] {c.Name}!")
            .WithDescription(
                $"📍 Pool: **{poolName}**\n" +
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                $"💰 Giá trị: **~{c.Coins:N0} coins** _(bán cá để nhận)_\n" +
                $"✨ +{result.FishingXpGained} Fishing XP\n" +
                $"🪣 Pool còn: **{result.PondRemaining:N0}** con" +
                bagStatus + pearlCapMsg + levelUpText)
            .WithColor(new Color(c.IsPearl ? 0xFFD700u : 0x1abc9cu))
            .Build();
    }

    // ── Leaderboard ───────────────────────────────────────────────────────────

    public static Embed BuildLeaderboardEmbed(
        LeaderboardSnapshot snapshot,
        SocketGuild guild,
        string weekLabel,
        HashSet<ulong>? excludeUserIds = null)
    {
        var (icon, unit, title) = snapshot.Type switch
        {
            LeaderboardType.Fishing => ("🎣", "cá",         "Bảng Xếp Hạng Ngư Thủ"),
            LeaderboardType.Chat    => ("💬", "tin nhắn",   "Bảng Xếp Hạng Chat"),
            LeaderboardType.Voice   => ("🎙️","phút voice", "Bảng Xếp Hạng Voice"),
            _ => ("📊", "", "Bảng Xếp Hạng")
        };

        // Lọc admin/owner — họ chơi riêng, không cạnh tranh bảng xếp hạng
        int displayRank = 1;
        var lines = snapshot.Entries
            .OrderBy(e => e.Rank)
            .Where(e => excludeUserIds is null || !excludeUserIds.Contains(e.UserId))
            .Select(e =>
            {
                var user   = guild.GetUser(e.UserId);
                var name   = user?.DisplayName ?? $"<@{e.UserId}>";
                var delta  = e.DeltaRank > 0 ? $"↑{e.DeltaRank}" : e.DeltaRank < 0 ? $"↓{Math.Abs(e.DeltaRank)}" : "─";
                var rank   = displayRank++;
                var medal  = rank <= 3 ? RankEmoji(rank) : $"`#{rank}`";
                return $"{medal} {name} — **{e.Value:N0}** {unit} `{delta}`";
            });

        return new EmbedBuilder()
            .WithTitle($"{icon} {title}")
            .WithDescription(string.Join("\n", lines))
            .WithColor(new Color(0xF5A623))
            .WithFooter($"Tuần {weekLabel} • Cập nhật Chủ Nhật 12:00 UTC")
            .Build();
    }

    // ── Achievements ──────────────────────────────────────────────────────────

    public static Embed BuildAchievementsEmbed(
        string username, List<UserFishingAchievement> achievements)
    {
        var desc = achievements.Count == 0
            ? "Chưa có thành tựu nào."
            : string.Join("\n", achievements.Select(a =>
                $"✅ **{a.AchievementId}** — <t:{new DateTimeOffset(a.CreatedAt).ToUnixTimeSeconds()}:D>"));

        return new EmbedBuilder()
            .WithTitle($"🏆 Thành Tựu Câu Cá của {username}")
            .WithDescription(desc)
            .WithColor(new Color(0xF5A623))
            .Build();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Color RarityColor(string rarity) => rarity switch
    {
        "Common"    => new Color(0x95a5a6),
        "Uncommon"  => new Color(0x2ecc71),
        "Rare"      => new Color(0x3498db),
        "Legendary" => new Color(0x9b59b6),
        "Mythic"    => new Color(0xF5A623),
        "Bronze"    => new Color(0xCD7F32),
        "Gold"      => new Color(0xFFD700),
        "Diamond"   => new Color(0xB9F2FF),
        "Trash"     => new Color(0x5d4037),
        _           => new Color(0x95a5a6)
    };

    private static string RarityVi(string rarity) => rarity switch
    {
        "Common"    => "Thường",
        "Uncommon"  => "Hiếm Vừa",
        "Rare"      => "Hiếm",
        "Legendary" => "Huyền Thoại",
        "Mythic"    => "Thần",
        "Bronze"    => "Đồng",
        "Gold"      => "Vàng",
        "Diamond"   => "Kim Cương",
        "Trash"     => "Rác",
        _           => rarity
    };

    private static string RankEmoji(int rank) => rank switch
    {
        1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{rank}"
    };

    private static string BuildProgressBar(double pct, int length)
    {
        var filled = (int)(pct / 100 * length);
        return "[" + new string('█', filled) + new string('░', length - filled) + "]";
    }

    private static string RarityEmoji(string rarity) => rarity switch
    {
        "Common"    => "⬜",
        "Uncommon"  => "🟩",
        "Rare"      => "🟦",
        "Legendary" => "🟪",
        "Mythic"    => "🟧",
        "Trash"     => "🟫",
        _           => "❓"
    };

    private static int RarityOrder(string rarity) => rarity switch
    {
        "Common"    => 1,
        "Uncommon"  => 2,
        "Rare"      => 3,
        "Legendary" => 4,
        "Mythic"    => 5,
        "Trash"     => 6,
        _           => 9
    };

    /// <summary>
    /// Embed nhỏ cho auto-fish khi bị miss hoặc cá thoát — post ra channel.
    /// </summary>
    public static Embed BuildAutoFishMissEmbed(string reason, string username)
        => new EmbedBuilder()
            .WithColor(new Color(0x636e72))
            .WithDescription($"🤖 **{username}** — {reason}")
            .Build();

    // ── Repair Rod ───────────────────────────────────────────────────────────

    public static Embed BuildRepairRodEmbed(
        InventoryItem item, int oldDur, int newDur, long cost, long coinsRemaining)
    {
        var pctBefore = (double)oldDur / newDur * 100;
        var barBefore = BuildProgressBar(pctBefore, 8);
        var barAfter  = BuildProgressBar(100, 8);

        var wasBroken    = oldDur == 0;
        var title        = wasBroken
            ? "🔧 Cần Câu Phục Hồi Hoàn Toàn!"
            : "🔧 Sửa Cần Câu Thành Công!";
        var statusBefore = wasBroken ? " 💔 **GÃY**"
            : oldDur <= newDur * 0.2 ? " ⚠️ Sắp gãy" : "";

        return new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(
                $"{item.Emoji} **{item.Name}**\n\n" +
                $"**Trước:** {barBefore} {oldDur}/{newDur}{statusBefore}\n" +
                $"**Sau:**   {barAfter} {newDur}/{newDur} ✅\n\n" +
                $"💸 Chi phí sửa: **{cost:N0}** xu\n" +
                $"💰 Số dư còn lại: **{coinsRemaining:N0}** xu")
            .WithColor(new Color(0x2ECC71))
            .WithFooter("🔧 Repair • Cần câu đã sẵn sàng câu tiếp!")
            .WithCurrentTimestamp()
            .Build();
    }

}