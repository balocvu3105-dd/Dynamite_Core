// src/Dynamite.Modules.Economy/Services/ShopShowcaseService.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Quản lý embed trưng bày cửa hàng trong ShopChannelId.
/// Bot post embed 1 lần, sau đó tự EDIT (không post mới) mỗi khi item thay đổi.
/// Message ID được lưu vào GuildConfig.ShopShowcaseMessageId để tái sử dụng.
/// </summary>
public class ShopShowcaseService
{
    private readonly IShopRepository       _shopRepo;
    private readonly IGuildConfigRepository _configRepo;
    private readonly DiscordSocketClient   _discord;
    private readonly ILogger<ShopShowcaseService> _logger;

    public ShopShowcaseService(
        IShopRepository        shopRepo,
        IGuildConfigRepository  configRepo,
        DiscordSocketClient    discord,
        ILogger<ShopShowcaseService> logger)
    {
        _shopRepo   = shopRepo;
        _configRepo = configRepo;
        _discord    = discord;
        _logger     = logger;
    }

    /// <summary>
    /// Set channel trưng bày, post embed và pin message.
    /// Gọi từ /shop set-channel.
    /// </summary>
    public async Task<(bool ok, string message)> SetChannelAsync(ulong guildId, string guildName, ITextChannel channel)
    {
        var config = await _configRepo.GetOrCreateAsync(guildId, guildName);

        // Xóa pin cũ nếu có
        if (config.ShopShowcaseMessageId.HasValue)
            await TryDeleteOldMessageAsync(guildId, config.ShopShowcaseMessageId.Value);

        config.ShopChannelId        = channel.Id;
        config.ShopShowcaseMessageId = null; // reset — sẽ tạo mới
        await _configRepo.SaveChangesAsync();

        // Post embed mới
        var messageId = await PostShowcaseAsync(guildId, channel);
        if (messageId is null)
            return (false, "❌ Không thể post embed vào channel đó.");

        config.ShopShowcaseMessageId = messageId;
        await _configRepo.SaveChangesAsync();

        // Pin message
        try
        {
            var msg = await channel.GetMessageAsync(messageId.Value) as IUserMessage;
            if (msg is not null) await msg.PinAsync();
        }
        catch { /* pin failed — non-critical */ }

        return (true, $"✅ Đã đặt **{channel.Mention}** làm phòng trưng bày cửa hàng và ghim embed.");
    }

    /// <summary>
    /// Cập nhật embed khi item thay đổi (add, seed, toggle).
    /// Gọi tự động — không throw.
    /// </summary>
    public async Task RefreshAsync(ulong guildId)
    {
        try
        {
            var config = await _configRepo.GetByGuildIdAsync(guildId);
            if (config?.ShopChannelId is null) return;

            var guild   = _discord.GetGuild(guildId);
            var channel = guild?.GetTextChannel(config.ShopChannelId.Value);
            if (channel is null) return;

            if (config.ShopShowcaseMessageId.HasValue)
            {
                // Edit message cũ
                var msg = await channel.GetMessageAsync(config.ShopShowcaseMessageId.Value) as IUserMessage;
                if (msg is not null)
                {
                    var embed = await BuildEmbedAsync(guildId);
                    await msg.ModifyAsync(p => p.Embed = embed);
                    return;
                }
            }

            // Message cũ không còn — post mới
            var newId = await PostShowcaseAsync(guildId, channel);
            if (newId.HasValue)
            {
                config.ShopShowcaseMessageId = newId;
                await _configRepo.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ShopShowcase] Failed to refresh for guild {GuildId}", guildId);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ulong?> PostShowcaseAsync(ulong guildId, ITextChannel channel)
    {
        try
        {
            var embed = await BuildEmbedAsync(guildId);
            var msg   = await channel.SendMessageAsync(embed: embed);
            return msg.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ShopShowcase] Failed to post for guild {GuildId}", guildId);
            return null;
        }
    }

    private async Task<Embed> BuildEmbedAsync(ulong guildId)
    {
        var items = await _shopRepo.GetAvailableItemsAsync(guildId);

        var builder = new EmbedBuilder()
            .WithTitle("🏪 Cửa Hàng Dynamite")
            .WithColor(new Color(0xF4D03F))
            .WithFooter("Dùng /shop buy <tên item> để mua • /shop inventory để xem túi đồ")
            .WithCurrentTimestamp();

        if (items.Count == 0)
        {
            builder.WithDescription("*Cửa hàng đang trống. Admin dùng `/shop seed` để thêm vật phẩm mặc định.*");
            return builder.Build();
        }

        // Nhóm theo loại
        var grouped = items
            .GroupBy(i => i.Type)
            .OrderBy(g => (int)g.Key);

        foreach (var group in grouped)
        {
            var header = group.Key switch
            {
                ItemType.FishingRod  => "🎣 Cần Câu",
                ItemType.Bait        => "🪱 Mồi Câu",
                ItemType.BagUpgrade  => "🎒 Nâng Túi Cá",
                ItemType.WeatherItem => "🌧️ Phép Thời Tiết",
                ItemType.PoolTicket  => "🎟️ Vé Special Pool",
                ItemType.AutoFish    => "🤖 Auto Câu Cá",
                ItemType.Consumable  => "🧪 Tiêu Thụ",
                _                   => "📦 Khác"
            };

            var lines = group.Select(item =>
            {
                var desc = item.Description is not null ? $"\n  ↳ *{item.Description}*" : string.Empty;
                // BagUpgrade dùng dynamic pricing — hiển thị giá khởi điểm (tier 1 = 10 slot đầu tiên)
                var displayPrice = item.Type == ItemType.BagUpgrade
                    ? ShopService.GetBagUpgradePrice(10) // giá cho túi 10 slot (mặc định)
                    : item.Price;
                return $"{item.Emoji} **{item.Name}** — 🪙 {displayPrice:N0} xu{desc}";
            });

            builder.AddField(header, string.Join("\n\n", lines), inline: false);
        }

        return builder.Build();
    }

    private async Task TryDeleteOldMessageAsync(ulong guildId, ulong messageId)
    {
        try
        {
            var config = await _configRepo.GetByGuildIdAsync(guildId);
            if (config?.ShopChannelId is null) return;
            var guild   = _discord.GetGuild(guildId);
            var channel = guild?.GetTextChannel(config.ShopChannelId.Value);
            if (channel is null) return;
            var msg = await channel.GetMessageAsync(messageId);
            if (msg is not null) await msg.DeleteAsync();
        }
        catch { /* ignore */ }
    }
}
