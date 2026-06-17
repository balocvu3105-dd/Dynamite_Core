// src/Dynamite.Modules.Economy/Commands/BagCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;

[Group("bag", "Quản lý túi cá của bạn")]
public class BagCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly FishBagService _bagService;

    public BagCommands(FishBagService bagService)
        => _bagService = bagService;

    // ── /bag view ─────────────────────────────────────────────────────────────

    [SlashCommand("view", "Xem túi cá của bạn")]
    public async Task ViewAsync()
    {
        var bag   = await _bagService.GetBagAsync(Context.Guild.Id, Context.User.Id);
        var embed = EconomyEmbedBuilder.BuildBagEmbed(bag, Context.User.GlobalName ?? Context.User.Username);
        await RespondAsync(embed: embed);
    }

    // ── /bag sell all ─────────────────────────────────────────────────────────

    [SlashCommand("sell-all", "Bán toàn bộ cá trong túi")]
    public async Task SellAllAsync()
    {
        await DeferAsync();

        var result = await _bagService.SellAllAsync(Context.Guild.Id, Context.User.Id);

        if (result.FishSold == 0)
        {
            await FollowupAsync("🎒 Túi đang trống, không có gì để bán!");
            return;
        }

        // Lấy số dư hiện tại để hiển thị
        var bag   = await _bagService.GetBagAsync(Context.Guild.Id, Context.User.Id);
        var embed = EconomyEmbedBuilder.BuildBagSellResultEmbed(result, walletTotal: result.CoinsEarned);
        await FollowupAsync(embed: embed);
    }

    // ── /bag sell ─────────────────────────────────────────────────────────────

    [SlashCommand("sell", "Bán cá theo độ hiếm")]
    public async Task SellByRarityAsync(
        [Summary("độ-hiếm", "Common / Uncommon / Rare / Legendary / Mythic")]
        [Choice("Thường (Common)",          "Common")]
        [Choice("Hiếm Vừa (Uncommon)",      "Uncommon")]
        [Choice("Hiếm (Rare)",              "Rare")]
        [Choice("Huyền Thoại (Legendary)",  "Legendary")]
        [Choice("Thần (Mythic)",            "Mythic")]
        string rarity)
    {
        await DeferAsync();

        var result = await _bagService.SellByRarityAsync(Context.Guild.Id, Context.User.Id, rarity);

        if (result.FishSold == 0)
        {
            await FollowupAsync($"Không có cá **{rarity}** trong túi!");
            return;
        }

        var embed = EconomyEmbedBuilder.BuildBagSellResultEmbed(result, result.CoinsEarned);
        await FollowupAsync(embed: embed);
    }
}
