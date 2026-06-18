// src/Dynamite.Modules.Economy/Commands/ItemCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Modules.Economy.Services;

/// <summary>
/// /item — Tương tác với vật phẩm tiêu thụ trong túi đồ.
/// </summary>
[Group("item", "Sử dụng vật phẩm trong túi đồ")]
[RequireContext(ContextType.Guild)]
public class ItemCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ShopService           _shop;
    private readonly WeatherForecastService _weatherForecast;
    private readonly InvoiceService        _invoice;

    public ItemCommands(
        ShopService            shop,
        WeatherForecastService  weatherForecast,
        InvoiceService         invoice)
    {
        _shop            = shop;
        _weatherForecast = weatherForecast;
        _invoice         = invoice;
    }

    // ── /item use <tên> ──────────────────────────────────────────────────────

    [SlashCommand("use", "Sử dụng vật phẩm tiêu thụ từ túi đồ (vd: Phép Triệu Mưa)")]
    public async Task UseAsync(
        [Summary("item", "Tên vật phẩm muốn dùng")] string itemName)
    {
        await DeferAsync(ephemeral: false); // public — thấy hiệu ứng

        var result = await _shop.UseItemAsync(Context.Guild.Id, Context.User.Id, itemName);

        if (!result)
        {
            await FollowupAsync($"❌ {result.ErrorMessage}", ephemeral: true);
            return;
        }

        var r = result.Value!;
        // Gửi result public
        await FollowupAsync($"<@{Context.User.Id}> {r.EffectDescription}");

        // Cập nhật forecast embed nếu là WeatherItem
        if (r.Item.Type == Core.Entities.ItemType.WeatherItem)
            await _weatherForecast.RefreshAsync(Context.Guild.Id);
    }
}
