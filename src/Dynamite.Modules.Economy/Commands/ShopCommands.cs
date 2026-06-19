// src/Dynamite.Modules.Economy/Commands/ShopCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;
using Dynamite.Core.Common.Results;

[Group("shop", "Shop and inventory commands")]
[RequireContext(ContextType.Guild)]
public class ShopCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ShopService             _shop;
    private readonly ShopShowcaseService     _showcase;
    private readonly InvoiceService          _invoice;
    private readonly WeatherForecastService  _weatherForecast;
    private readonly IGuildConfigRepository  _configRepo;
    private readonly FishBagService          _bagService;
    private readonly IUserProfileRepository  _profileRepo;

    public ShopCommands(
        ShopService            shop,
        ShopShowcaseService    showcase,
        InvoiceService         invoice,
        WeatherForecastService weatherForecast,
        IGuildConfigRepository configRepo,
        FishBagService         bagService,
        IUserProfileRepository profileRepo)
    {
        _shop            = shop;
        _showcase        = showcase;
        _invoice         = invoice;
        _weatherForecast = weatherForecast;
        _configRepo      = configRepo;
        _bagService      = bagService;
        _profileRepo     = profileRepo;
    }

    // ── /shop view ─────────────────────────────────────────────────────────────

    [SlashCommand("view", "Xem danh sách vật phẩm trong cửa hàng")]
    public async Task ViewAsync()
    {
        await DeferAsync(ephemeral: true);
        var items = await _shop.GetShopItemsAsync(Context.Guild.Id);
        var bag   = await _bagService.GetBagAsync(Context.Guild.Id, Context.User.Id);
        var bagUpgradePrice = ShopService.GetBagUpgradePrice(bag.BagCapacity);
        var embed = EconomyEmbedBuilder.BuildShopEmbed(items, bagUpgradePrice);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── /shop buy ──────────────────────────────────────────────────────────────

    [SlashCommand("buy", "Mua vật phẩm từ cửa hàng")]
    public async Task BuyAsync(
        [Summary("item", "Tên vật phẩm muốn mua")] string itemName)
    {
        await DeferAsync(ephemeral: true);

        var result = await _shop.BuyWithDetailsAsync(Context.Guild.Id, Context.User.Id, itemName);

        if (!result)
        {
            await FollowupAsync($"❌ {result.ErrorMessage}", ephemeral: true);
            return;
        }

        var r = result.Value!;
        await FollowupAsync(r.DisplayMessage, ephemeral: true);

        // Gửi hóa đơn vào invoice channel + DM người mua
        await _invoice.SendAsync(Context.Guild.Id, Context.User.Id, r.Item, r.CoinsPaid, r.CoinsRemaining);
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

        var result = await _shop.UseItemAsync(Context.Guild.Id, Context.User.Id, itemName);

        var response = result
            ? result.Value!.EffectDescription
            : $"❌ {result.ErrorMessage}";
        await FollowupAsync(response, ephemeral: true);
    }

    // ── /shop repair-rod ──────────────────────────────────────────────────────

    [SlashCommand("repair-rod", "Sửa chữa cần câu bị mòn hoặc gãy")]
    public async Task RepairRodAsync(
        [Summary("rod", "Tên cần câu muốn sửa (để trống = tự chọn cần hư nhất)")]
        [Autocomplete(typeof(RodAutocomplete))]
        string? rodName = null,
        [Summary("preview", "Xem chi phí trước khi sửa — không trừ xu (mặc định: false)")]
        bool preview = false)
    {
        await DeferAsync(ephemeral: true);

        // ── Preview mode: tính chi phí, không sửa ────────────────────────────
        if (preview)
        {
            var previewResult = await _shop.PreviewRepairAsync(
                Context.Guild.Id, Context.User.Id, rodName);

            if (!previewResult)
            {
                await FollowupAsync($"❌ {previewResult.ErrorMessage}", ephemeral: true);
                return;
            }

            var pv = previewResult.Value!;
            var previewEmbed = EconomyEmbedBuilder.BuildRepairPreviewEmbed(
                pv.Rod.Item,
                pv.Rod.RodDurability!.Value,
                pv.Rod.Item.MaxDurability!.Value,
                pv.Cost, pv.Coins);

            await FollowupAsync(embed: previewEmbed, ephemeral: true);
            return;
        }

        // ── Repair mode: thực hiện sửa, trừ xu ───────────────────────────────
        var result = await _shop.RepairRodAsync(Context.Guild.Id, Context.User.Id, rodName);

        if (!result)
        {
            await FollowupAsync($"❌ {result.ErrorMessage}", ephemeral: true);
            return;
        }

        var r     = result.Value!;
        var embed = EconomyEmbedBuilder.BuildRepairRodEmbed(
            r.Item, r.OldDurability, r.NewDurability, r.CoinsPaid, r.CoinsRemaining);
        await FollowupAsync(embed: embed, ephemeral: true);

        await _invoice.SendAsync(Context.Guild.Id, Context.User.Id,
            r.Item, r.CoinsPaid, r.CoinsRemaining);
    }

    // ── /shop seed (Admin) ─────────────────────────────────────────────────────

    [SlashCommand("seed", "Seed vật phẩm mặc định vào cửa hàng (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SeedAsync()
    {
        await DeferAsync(ephemeral: true);

        var (added, updated) = await _shop.SeedDefaultItemsAsync(Context.Guild.Id);

        var reply = (added, updated) switch
        {
            (0, 0) => "ℹ️ Tất cả vật phẩm đã up-to-date.",
            (0, _) => $"✅ Đã sync **{updated}** vật phẩm — giá và stats đã được cập nhật.",
            (_, 0) => $"✅ Đã thêm **{added}** vật phẩm mới.",
            _      => $"✅ Thêm **{added}** mới · Cập nhật **{updated}** vật phẩm."
        };

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

        var result = await _shop.AddShopItemAsync(
            Context.Guild.Id, name, emoji, price, description, type,
            cooldown, multiplier, usageCount: null, durationMinutes: null);

        var response = result
            ? $"✅ Đã thêm **{name}** vào cửa hàng."
            : $"❌ {result.ErrorMessage}";
        await FollowupAsync(response, ephemeral: true);

        // Cập nhật showcase
        if (result) await _showcase.RefreshAsync(Context.Guild.Id);
    }

    // ── Admin: channel setup ──────────────────────────────────────────────────

    [SlashCommand("set-channel", "Đặt kênh trưng bày cửa hàng — bot tự post và pin embed (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetShowcaseChannelAsync(
        [Summary("channel", "Channel trưng bày cho mọi người xem")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var result = await _showcase.SetChannelAsync(Context.Guild.Id, Context.Guild.Name, channel);

        var response = result
            ? $"✅ Đã đặt {channel.Mention} làm phòng trưng bày cửa hàng và ghim embed."
            : $"❌ {result.ErrorMessage}";
        await FollowupAsync(response, ephemeral: true);
    }

    // ── /shop upgrade-rod ─────────────────────────────────────────────────────

    [SlashCommand("upgrade-rod", "Nâng cấp cần câu Bạc → Vàng hoặc Vàng → Kim Cương")]
    public async Task UpgradeRodAsync(
        [Summary("rod", "Cần câu muốn nâng cấp (Cần Câu Bạc / Cần Câu Vàng)")]
        [Autocomplete(typeof(RodAutocomplete))]
        string rodName)
    {
        await DeferAsync(ephemeral: true);

        // Lấy LegendaryCaught từ profile (không inject thêm repo vào ShopService)
        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        var result = await _shop.UpgradeRodAsync(
            Context.Guild.Id, Context.User.Id,
            rodName, profile.LegendaryCaught);

        if (!result)
        {
            await FollowupAsync($"❌ {result.ErrorMessage}", ephemeral: true);
            return;
        }

        var r     = result.Value!;
        var embed = EconomyEmbedBuilder.BuildRodUpgradeEmbed(r);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── Admin: channel setup ──────────────────────────────────────────────────

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

        var result = await _weatherForecast.SetChannelAsync(
            Context.Guild.Id, Context.Guild.Name, channel);

        var response = result
            ? $"✅ Đã đặt {channel.Mention} làm kênh dự báo thời tiết."
            : $"❌ {result.ErrorMessage}";
        await FollowupAsync(response, ephemeral: true);
    }
}
