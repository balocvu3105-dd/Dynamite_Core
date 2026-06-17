// src/Dynamite.Modules.Economy/Commands/ShopCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Core.Entities;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;

[Group("shop", "Shop and inventory commands")]
public class ShopCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ShopService _shop;

    public ShopCommands(ShopService shop)
    {
        _shop = shop;
    }

    [SlashCommand("view", "Browse the shop")]
    public async Task ViewAsync()
    {
        await DeferAsync(ephemeral: true);

        var items = await _shop.GetShopItemsAsync(Context.Guild.Id);
        var embed = EconomyEmbedBuilder.BuildShopEmbed(items);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("buy", "Purchase an item from the shop")]
    public async Task BuyAsync(
        [Summary("item", "Name of the item to buy")] string itemName)
    {
        await DeferAsync(ephemeral: true);

        var (success, message) = await _shop.BuyAsync(Context.Guild.Id, Context.User.Id, itemName);
        await FollowupAsync(success ? message : $"❌ {message}", ephemeral: true);
    }

    [SlashCommand("inventory", "View your inventory")]
    public async Task InventoryAsync()
    {
        await DeferAsync(ephemeral: true);

        var items = await _shop.GetInventoryAsync(Context.Guild.Id, Context.User.Id);
        var embed = EconomyEmbedBuilder.BuildInventoryEmbed(items, Context.User.Username);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("seed", "Seed vật phẩm mặc định vào cửa hàng (Admin only)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SeedAsync()
    {
        await DeferAsync(ephemeral: true);

        var added = await _shop.SeedDefaultItemsAsync(Context.Guild.Id);

        if (added == 0)
            await FollowupAsync("ℹ️ Tất cả vật phẩm mặc định đã tồn tại trong cửa hàng rồi.", ephemeral: true);
        else
            await FollowupAsync($"✅ Đã thêm **{added}** vật phẩm mặc định vào cửa hàng!\nDùng `/shop view` để xem.", ephemeral: true);
    }

    [SlashCommand("additem", "Add an item to the shop (Admin only)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task AddItemAsync(
        [Summary("name", "Item name")] string name,
        [Summary("price", "Price in coins")] long price,
        [Summary("emoji", "Item emoji")] string emoji,
        [Summary("type", "Item type")] ItemType type = ItemType.Collectible,
        [Summary("description", "Item description")] string? description = null,
        [Summary("cooldown", "Fishing cooldown in seconds (FishingRod only)")] int? cooldown = null,
        [Summary("multiplier", "Drop multiplier (FishingRod only)")] double? multiplier = null)
    {
        await DeferAsync(ephemeral: true);

        var (success, message) = await _shop.AddShopItemAsync(
            Context.Guild.Id, name, emoji, price, description, type,
            cooldown, multiplier, usageCount: null, durationMinutes: null);

        await FollowupAsync(success ? message : $"❌ {message}", ephemeral: true);
    }
}