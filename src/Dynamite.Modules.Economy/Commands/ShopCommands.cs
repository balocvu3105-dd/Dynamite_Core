// src/Dynamite.Modules.Economy/Commands/ShopCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;

[Group("shop", "Shop and inventory commands")]
[RequireContext(ContextType.Guild)]
public class ShopCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ShopService            _shop;
    private readonly ShopShowcaseService    _showcase;
    private readonly InvoiceService         _invoice;
    private readonly WeatherForecastService _weatherForecast;
    private readonly IGuildConfigRepository _configRepo;

    public ShopCommands(
        ShopService            shop,
        ShopShowcaseService    showcase,
        InvoiceService         invoice,
        WeatherForecastService weatherForecast,
        IGuildConfigRepository configRepo)
    {
        _shop            = shop;
        _showcase        = showcase;
        _invoice         = invoice;
        _weatherForecast = weatherForecast;
        _configRepo      = configRepo;
    }

    // ── /shop view ─────────────────────────────────────────────────────────────

    [SlashCommand("view", "Xem danh sách vật phẩm trong cửa hàng")]
    public async Task ViewAsync()
    {
        await DeferAsync(ephemeral: true);
        var items = await _shop.GetShopItemsAsync(Context.Guild.Id);
        var embed = EconomyEmbedBuilder.BuildShopEmbed(items);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── /shop buy ──────────────────────────────────────────────────────────────

    [SlashCommand("buy", "Mua vật phẩm từ cửa hàng")]
    public async Task BuyAsync(
        [Summary("item", "Tên vật phẩm muốn mua")] string itemName)
    {
        await DeferAsync(ephemeral: true);

        var (success, message, item, coinsPaid, coinsRemaining) =
            await _shop.BuyWithDetailsAsync(Context.Guild.Id, Context.User.Id, itemName);

        await FollowupAsync(success ? message : $"❌ {message}", ephemeral: true);

        // Gửi hóa đơn vào invoice channel + DM người mua
        if (success && item is not null)
            await _invoice.SendAsync(Context.Guild.Id, Context.User.Id, item, coinsPaid, coinsRemaining);
    }

    // ── /shop inventory ────────────────────────────────────────────────────────

    [SlashCommand("inventory", "Xem túi đồ của bạn")]
    public async Task InventoryAsync()
    {
        await DeferAsync(ephemeral: true);
        var items = await _shop.GetInventoryAsync(Context.Guild.Id, Context.User.Id);
        var embed = EconomyEmbedBuilder.BuildInventoryEmbed(items, Context.User.Username);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── /shop use ──────────────────────────────────────────────────────────────

    [SlashCommand("use", "Sử dụng vật phẩm tiêu thụ trong túi đồ")]
    public async Task UseAsync(
        [Summary("item", "Tên vật phẩm muốn dùng (vd: Phép Triệu Mưa)")] string itemName)
    {
        await DeferAsync(ephemeral: true);

        var (success, message, _) =
            await _shop.UseItemAsync(Context.Guild.Id, Context.User.Id, itemName);

        await FollowupAsync(success ? message : $"❌ {message}", ephemeral: true);
    }

    // ── /shop seed (Admin) ─────────────────────────────────────────────────────

    [SlashCommand("seed", "Seed vật phẩm mặc định vào cửa hàng (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SeedAsync()
    {
        await DeferAsync(ephemeral: true);

        var added = await _shop.SeedDefaultItemsAsync(Context.Guild.Id);

        var reply = added == 0
            ? "ℹ️ Tất cả vật phẩm mặc định đã tồn tại trong cửa hàng rồi."
            : $"✅ Đã thêm **{added}** vật phẩm mặc định! Dùng `/shop view` để xem.";

        await FollowupAsync(reply, ephemeral: true);

        // Tự cập nhật showcase embed
        await _showcase.RefreshAsync(Context.Guild.Id);
    }

    // ── /shop additem (Admin) ──────────────────────────────────────────────────

    [SlashCommand("additem", "Thêm vật phẩm vào cửa hàng (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task AddItemAsync(
        [Summary("name", "Tên vật phẩm")] string name,
        [Summary("price", "Giá (xu)")] long price,
        [Summary("emoji", "Emoji vật phẩm")] string emoji,
        [Summary("type", "Loại vật phẩm")] ItemType type = ItemType.Collectible,
        [Summary("description", "Mô tả")] string? description = null,
        [Summary("cooldown", "Cooldown câu cá (giây, chỉ FishingRod)")] int? cooldown = null,
        [Summary("multiplier", "Drop multiplier (chỉ FishingRod)")] double? multiplier = null)
    {
        await DeferAsync(ephemeral: true);

        var (success, message) = await _shop.AddShopItemAsync(
            Context.Guild.Id, name, emoji, price, description, type,
            cooldown, multiplier, usageCount: null, durationMinutes: null);

        await FollowupAsync(success ? message : $"❌ {message}", ephemeral: true);

        // Cập nhật showcase
        if (success) await _showcase.RefreshAsync(Context.Guild.Id);
    }

    // ── Admin: channel setup ──────────────────────────────────────────────────

    [SlashCommand("set-channel", "Đặt kênh trưng bày cửa hàng — bot tự post và pin embed (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetShowcaseChannelAsync(
        [Summary("channel", "Channel trưng bày cho mọi người xem")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var (ok, msg) = await _showcase.SetChannelAsync(
            Context.Guild.Id, Context.Guild.Name, channel);

        await FollowupAsync(msg, ephemeral: true);
    }

    [SlashCommand("set-invoice-channel", "Đặt kênh hóa đơn giao dịch (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetInvoiceChannelAsync(
        [Summary("channel", "Channel nhận hóa đơn public mỗi khi có giao dịch")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);
        config.InvoiceChannelId = channel.Id;
        await _configRepo.SaveChangesAsync();

        await FollowupAsync($"✅ Hóa đơn mua bán sẽ được gửi vào {channel.Mention}.", ephemeral: true);
    }

    [SlashCommand("set-weather-channel", "Đặt kênh dự báo thời tiết bể cá (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetWeatherChannelAsync(
        [Summary("channel", "Channel dự báo thời tiết — bot ghim embed và tự update")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var (ok, msg) = await _weatherForecast.SetChannelAsync(
            Context.Guild.Id, Context.Guild.Name, channel);

        await FollowupAsync(msg, ephemeral: true);
    }
}
