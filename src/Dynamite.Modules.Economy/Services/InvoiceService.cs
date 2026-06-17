// src/Dynamite.Modules.Economy/Services/InvoiceService.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Gửi hóa đơn mua bán:
///  1. Post vào InvoiceChannelId (public — ai cũng thấy)
///  2. DM riêng người mua
/// Gọi từ ShopCommands sau khi BuyAsync thành công.
/// </summary>
public class InvoiceService
{
    private readonly IGuildConfigRepository _configRepo;
    private readonly DiscordSocketClient    _discord;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IGuildConfigRepository configRepo,
        DiscordSocketClient    discord,
        ILogger<InvoiceService> logger)
    {
        _configRepo = configRepo;
        _discord    = discord;
        _logger     = logger;
    }

    /// <summary>
    /// Gửi hóa đơn sau khi mua thành công.
    /// Non-throwing — lỗi chỉ log, không ảnh hưởng giao dịch.
    /// </summary>
    public async Task SendAsync(
        ulong guildId, ulong userId,
        InventoryItem item, long coinsPaid, long coinsRemaining)
    {
        var embed = BuildInvoiceEmbed(userId, item, coinsPaid, coinsRemaining);

        await Task.WhenAll(
            SendToInvoiceChannelAsync(guildId, embed),
            SendDmAsync(userId, embed));
    }

    // ── Embed builder ─────────────────────────────────────────────────────────

    private static Embed BuildInvoiceEmbed(
        ulong userId, InventoryItem item, long coinsPaid, long coinsRemaining)
    {
        var typeLabel = item.Type switch
        {
            ItemType.FishingRod  => "Cần Câu",
            ItemType.Bait        => "Mồi Câu",
            ItemType.BagUpgrade  => "Nâng Túi",
            ItemType.WeatherItem => "Phép Thời Tiết",
            ItemType.PoolTicket  => "Vé Special Pool",
            ItemType.AutoFish    => "Auto Câu Cá",
            _                   => "Vật phẩm"
        };

        return new EmbedBuilder()
            .WithTitle("🧾 Hóa Đơn Mua Hàng")
            .WithColor(new Color(0x2ECC71))
            .AddField("Người mua", $"<@{userId}>", inline: true)
            .AddField("Loại", typeLabel, inline: true)
            .AddField("​", "​", inline: true) // spacer
            .AddField("Vật phẩm", $"{item.Emoji} **{item.Name}**", inline: true)
            .AddField("Đã trả", $"🪙 **{coinsPaid:N0}** xu", inline: true)
            .AddField("Số dư còn lại", $"🪙 {coinsRemaining:N0} xu", inline: true)
            .WithFooter("Dynamite Shop • Giao dịch thành công")
            .WithCurrentTimestamp()
            .Build();
    }

    // ── Send helpers ──────────────────────────────────────────────────────────

    private async Task SendToInvoiceChannelAsync(ulong guildId, Embed embed)
    {
        try
        {
            var config = await _configRepo.GetByGuildIdAsync(guildId);
            if (config?.InvoiceChannelId is null) return;

            var guild   = _discord.GetGuild(guildId);
            var channel = guild?.GetTextChannel(config.InvoiceChannelId.Value);
            if (channel is null) return;

            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Invoice] Failed to post to invoice channel for guild {GuildId}", guildId);
        }
    }

    private async Task SendDmAsync(ulong userId, Embed embed)
    {
        try
        {
            var user = _discord.GetUser(userId);
            if (user is null) return;

            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            // DM thất bại (user tắt DM) — không ảnh hưởng giao dịch
            _logger.LogDebug(ex, "[Invoice] Failed to DM user {UserId}", userId);
        }
    }
}
