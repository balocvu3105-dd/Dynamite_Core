// src/Dynamite.Modules.Economy/Helpers/EconomyEmbedBuilder.cs
namespace Dynamite.Modules.Economy.Helpers;

using Discord;
using Dynamite.Core.Entities;

public static class EconomyEmbedBuilder
{
    public static Embed BuildBalanceEmbed(UserWallet wallet, string username)
    {
        return new EmbedBuilder()
            .WithTitle($"🪙 {username}'s Wallet")
            .WithDescription($"**Balance:** {wallet.Coins:N0} coins")
            .WithColor(new Color(0xF5A623))
            .AddField("📅 Daily Streak", $"{wallet.DailyStreak} days", inline: true)
            .AddField("📆 Last Daily",
                wallet.LastDaily.HasValue
                    ? $"<t:{new DateTimeOffset(wallet.LastDaily.Value).ToUnixTimeSeconds()}:R>"
                    : "Never",
                inline: true)
            .Build();
    }

    public static Embed BuildDailyEmbed(long coinsEarned, long total, int streak)
    {
        var streakBonus = streak > 1 ? $" (🔥 {streak} day streak!)" : "";
        return new EmbedBuilder()
            .WithTitle("📅 Daily Reward")
            .WithDescription($"You claimed **{coinsEarned:N0} coins**{streakBonus}!\n\n💰 Balance: **{total:N0} coins**")
            .WithColor(new Color(0x57F287))
            .WithFooter("Come back tomorrow to keep your streak!")
            .Build();
    }

    public static Embed BuildFishEmbed(FishCatch result, long total, string? rodName)
    {
        var rodInfo = rodName != null ? $"\n🎣 Rod: {rodName}" : "";
        return new EmbedBuilder()
            .WithTitle($"{result.Emoji} You caught a {result.Name}!")
            .WithDescription(
                $"**Rarity:** {result.Rarity}\n" +
                $"**Earned:** {result.Coins:N0} coins\n" +
                $"💰 Balance: **{total:N0} coins**" +
                rodInfo)
            .WithColor(result.Rarity switch
            {
                "Common"    => new Color(0x95a5a6),
                "Uncommon"  => new Color(0x2ecc71),
                "Rare"      => new Color(0x3498db),
                "Legendary" => new Color(0x9b59b6),
                "Mythic"    => new Color(0xF5A623),
                _           => new Color(0x95a5a6)
            })
            .Build();
    }

    public static Embed BuildLeaderboardEmbed(List<(int rank, ulong userId, long coins)> entries, ulong guildId)
    {
        var desc = entries.Count == 0
            ? "No data yet."
            : string.Join("\n", entries.Select(e =>
                $"{RankEmoji(e.rank)} <@{e.userId}> — **{e.coins:N0}** coins"));

        return new EmbedBuilder()
            .WithTitle("🏆 Coin Leaderboard")
            .WithDescription(desc)
            .WithColor(new Color(0xF5A623))
            .Build();
    }

    public static Embed BuildShopEmbed(List<InventoryItem> items)
    {
        var desc = items.Count == 0
            ? "No items available."
            : string.Join("\n\n", items.Select(i =>
                $"{i.Emoji} **{i.Name}** — {i.Price:N0} coins\n" +
                $"_{i.Description ?? "No description."}_" +
                (i.Type == ItemType.FishingRod
                    ? $"\n🎣 Cooldown: {i.CooldownSeconds}s | Multiplier: x{i.DropMultiplier:F1}"
                    : "")));

        return new EmbedBuilder()
            .WithTitle("🛒 Shop")
            .WithDescription(desc)
            .WithColor(new Color(0x5865F2))
            .WithFooter("Use /buy <item name> to purchase")
            .Build();
    }

    public static Embed BuildInventoryEmbed(List<UserInventory> items, string username)
    {
        var desc = items.Count == 0
            ? "Your inventory is empty."
            : string.Join("\n", items.Select(i =>
                $"{i.Item.Emoji} **{i.Item.Name}** x{i.Quantity}"));

        return new EmbedBuilder()
            .WithTitle($"🎒 {username}'s Inventory")
            .WithDescription(desc)
            .WithColor(new Color(0x5865F2))
            .Build();
    }

    private static string RankEmoji(int rank) => rank switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $"#{rank}"
    };
}