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
        var c = result.Catch;
        var weather = WeatherService.GetWeatherEmoji(result.Weather);
        var rodInfo  = result.RodName != null ? $"🎣 Cần: {result.RodName}\n" : "";
        var xpInfo   = $"✨ +{result.FishingXpGained} Fishing XP\n";
        var pondInfo = $"🪣 Bể còn: **{result.PondRemaining:N0}** con";

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

        var chestLabel = c.IsChest ? "Hòm Báu" : "Cá";
        return new EmbedBuilder()
            .WithTitle($"{c.Emoji} Bắt được {chestLabel}: {c.Name}!")
            .WithDescription(
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                $"**Tiền:** +{c.Coins:N0} coins\n" +
                $"💰 Số dư: **{result.TotalCoins:N0} coins**\n\n" +
                $"{rodInfo}{xpInfo}{pondInfo} {weather}" +
                levelUpText + achieveText)
            .WithColor(RarityColor(c.Rarity))
            .Build();
    }

    /// <summary>
    /// Embed cho auto-fish (user mode) — có username và countdown.
    /// </summary>
    public static Embed BuildAutoFishEmbed(FishResult result, DateTime expiresAt, string username)
    {
        var c        = result.Catch;
        var weather  = WeatherService.GetWeatherEmoji(result.Weather);
        var rodInfo  = result.RodName != null ? $"🎣 Cần: {result.RodName}\n" : "";
        var xpInfo   = $"✨ +{result.FishingXpGained} Fishing XP\n";
        var pondInfo = $"🪣 Bể còn: **{result.PondRemaining:N0}** con";

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

        var remaining    = expiresAt - DateTime.UtcNow;
        var countdownStr = remaining.TotalSeconds > 0 ? FormatRemaining(remaining) : "Hết hạn";
        var chestLabel   = c.IsChest ? "Hòm Báu" : "Cá";

        return new EmbedBuilder()
            .WithTitle($"🤖 [Auto] {username} {c.Emoji} Bắt được {chestLabel}: {c.Name}!")
            .WithDescription(
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                $"**Tiền:** +{c.Coins:N0} coins\n" +
                $"💰 Số dư: **{result.TotalCoins:N0} coins**\n\n" +
                $"{rodInfo}{xpInfo}{pondInfo} {weather}" +
                levelUpText + achieveText)
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
        var weather  = WeatherService.GetWeatherEmoji(result.Weather);
        var rodInfo  = result.RodName != null ? $"🎣 Cần: {result.RodName}\n" : "";
        var xpInfo   = $"✨ +{result.FishingXpGained} Fishing XP\n";
        var pondInfo = $"🪣 Bể còn: **{result.PondRemaining:N0}** con";

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

        var chestLabel = c.IsChest ? "Hòm Báu" : "Cá";

        return new EmbedBuilder()
            .WithTitle($"🛠️ [Admin Auto] {username} {c.Emoji} Bắt được {chestLabel}: {c.Name}!")
            .WithDescription(
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                $"**Tiền:** +{c.Coins:N0} coins\n" +
                $"💰 Số dư: **{result.TotalCoins:N0} coins**\n\n" +
                $"{rodInfo}{xpInfo}{pondInfo} {weather}" +
                levelUpText + achieveText)
            .WithColor(RarityColor(c.Rarity))
            .WithFooter("🛠️ Admin Auto-Fish")
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
        var weather = WeatherService.GetWeatherEmoji(status.Weather);
        var weatherExp = new DateTimeOffset(status.WeatherExpiresAt).ToUnixTimeSeconds();

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

        return new EmbedBuilder()
            .WithTitle("🌊 Trạng Thái Bể Cá")
            .WithDescription(
                $"{pondDesc}\n\n" +
                $"**Thời tiết:** {weather} {status.Weather}\n" +
                $"Thay đổi: <t:{weatherExp}:R>")
            .WithColor(status.IsEmpty ? new Color(0xED4245) : new Color(0x3498db))
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

    public static Embed BuildShopEmbed(List<InventoryItem> items)
    {
        var desc = items.Count == 0
            ? "Cửa hàng đang trống."
            : string.Join("\n\n", items.Select(BuildShopItemLine));

        return new EmbedBuilder()
            .WithTitle("🛒 Cửa Hàng")
            .WithDescription(desc)
            .WithColor(new Color(0x5865F2))
            .WithFooter("Dùng /shop buy <tên vật phẩm> để mua")
            .Build();
    }

    private static string BuildShopItemLine(InventoryItem i)
    {
        var line = $"{i.Emoji} **{i.Name}** — {i.Price:N0} coins\n_{i.Description ?? "Không có mô tả."}_";
        return i.Type switch
        {
            ItemType.FishingRod  => line + $"\n🎣 Cooldown: {i.CooldownSeconds}s | Nhân: x{i.DropMultiplier:F1}",
            ItemType.Bait        => line + $"\n🪱 +10% Rare | {i.UsageCount} lần dùng",
            ItemType.AutoFish    => line + $"\n🤖 Auto câu {i.DurationMinutes} phút (10s/lần)",
            ItemType.WeatherItem => line + $"\n☔ Force Rainy {i.DurationMinutes} phút",
            ItemType.PoolTicket  => line + $"\n🎟️ Vào Special Pool 1 lần | Yêu cầu Level 20+",
            _                    => line
        };
    }

    public static Embed BuildInventoryEmbed(List<UserInventory> items, string username)
    {
        var desc = items.Count == 0
            ? "Kho đồ đang trống."
            : string.Join("\n", items.Select(i =>
                $"{i.Item.Emoji} **{i.Item.Name}** x{i.Quantity}"));

        return new EmbedBuilder()
            .WithTitle($"🎒 Kho Đồ của {username}")
            .WithDescription(desc)
            .WithColor(new Color(0x5865F2))
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
            ? $"\n📦 Đã lưu vào túi ({result.BagFreeSlots} slot còn lại)"
            : "\n📦 Túi đầy — cá rơi xuống biển (vẫn nhận coins)";

        return new EmbedBuilder()
            .WithTitle($"{c.Emoji} [Pool Đặc Biệt] {c.Name}!")
            .WithDescription(
                $"📍 Pool: **{poolName}**\n" +
                $"**Độ hiếm:** {RarityVi(c.Rarity)}\n" +
                $"**Tiền:** +{c.Coins:N0} coins\n" +
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
        _           => "❓"
    };

    private static int RarityOrder(string rarity) => rarity switch
    {
        "Common"    => 1,
        "Uncommon"  => 2,
        "Rare"      => 3,
        "Legendary" => 4,
        "Mythic"    => 5,
        _           => 9
    };
}
