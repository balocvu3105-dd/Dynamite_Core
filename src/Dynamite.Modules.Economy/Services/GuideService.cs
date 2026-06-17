// src/Dynamite.Modules.Economy/Services/GuideService.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Đăng các embed hướng dẫn preset vào GuideChannelId.
/// Mỗi lần /guide post sẽ xóa các embed cũ và đăng lại.
/// </summary>
public class GuideService
{
    private readonly IGuildConfigRepository _configRepo;
    private readonly DiscordSocketClient    _discord;
    private readonly ILogger<GuideService>  _logger;

    public GuideService(
        IGuildConfigRepository configRepo,
        DiscordSocketClient    discord,
        ILogger<GuideService>  logger)
    {
        _configRepo = configRepo;
        _discord    = discord;
        _logger     = logger;
    }

    /// <summary>Set channel cẩm nang.</summary>
    public async Task<(bool ok, string message)> SetChannelAsync(
        ulong guildId, string guildName, ITextChannel channel)
    {
        var config = await _configRepo.GetOrCreateAsync(guildId, guildName);
        config.GuideChannelId = channel.Id;
        await _configRepo.SaveChangesAsync();
        return (true, $"✅ Đã đặt {channel.Mention} làm kênh cẩm nang hướng dẫn. Dùng `/guide post` để đăng nội dung.");
    }

    /// <summary>
    /// Đăng tất cả embed hướng dẫn vào channel đã cấu hình.
    /// Trả về số embed đã đăng.
    /// </summary>
    public async Task<(bool ok, string message)> PostGuideAsync(ulong guildId)
    {
        var config = await _configRepo.GetByGuildIdAsync(guildId);
        if (config?.GuideChannelId is null)
            return (false, "❌ Chưa đặt kênh cẩm nang. Dùng `/guide set-channel` trước.");

        var guild   = _discord.GetGuild(guildId);
        var channel = guild?.GetTextChannel(config.GuideChannelId.Value);
        if (channel is null)
            return (false, "❌ Không tìm thấy channel. Kiểm tra lại `/guide set-channel`.");

        try
        {
            // Xóa lịch sử cũ trong channel (tối đa 50 messages)
            var old = await channel.GetMessagesAsync(50).FlattenAsync();
            var botMessages = old.Where(m => m.Author.Id == _discord.CurrentUser.Id).ToList();
            foreach (var m in botMessages)
            {
                try { await m.DeleteAsync(); } catch { /* ignore */ }
                await Task.Delay(300); // rate limit
            }

            // Đăng header
            await channel.SendMessageAsync(embed: BuildHeaderEmbed());
            await Task.Delay(500);

            // Đăng từng section
            var sections = BuildGuideSections();
            foreach (var embed in sections)
            {
                await channel.SendMessageAsync(embed: embed);
                await Task.Delay(500);
            }

            _logger.LogInformation("[Guide] Posted {Count} embeds to guild {GuildId}", sections.Count + 1, guildId);
            return (true, $"✅ Đã đăng **{sections.Count + 1}** embed hướng dẫn vào {channel.Mention}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Guide] Failed to post for guild {GuildId}", guildId);
            return (false, $"❌ Lỗi khi đăng hướng dẫn: `{ex.Message}`");
        }
    }

    // ── Embed builders ─────────────────────────────────────────────────────────

    private static Embed BuildHeaderEmbed() =>
        new EmbedBuilder()
            .WithTitle("📖 Cẩm Nang Dynamite — Hướng Dẫn Toàn Tập")
            .WithDescription(
                "Chào mừng bạn đến với **Dynamite** — server fishing & economy!\n\n" +
                "Dưới đây là hướng dẫn đầy đủ để bắt đầu. " +
                "Đọc từng phần và bắt đầu hành trình của bạn! 🎣")
            .WithColor(new Color(0xE67E22))
            .WithImageUrl("https://i.imgur.com/placeholder.png") // thay bằng banner server nếu có
            .WithFooter("Dynamite Bot • Cập nhật thường xuyên")
            .WithCurrentTimestamp()
            .Build();

    private static List<Embed> BuildGuideSections() =>
    [
        // ── Section 1: Economy cơ bản ────────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("💰 Economy — Kiếm Xu")
            .WithColor(new Color(0xF1C40F))
            .WithDescription("Xu là tiền tệ chính trong server. Dùng xu để mua vật phẩm, vé pool đặc biệt và nhiều thứ khác.")
            .AddField("🎯 Nhận xu hằng ngày",
                "`/daily` — Nhận xu điểm danh hằng ngày\n" +
                "Streak liên tiếp giúp bonus nhiều hơn!", inline: false)
            .AddField("💼 Kiểm tra tài khoản",
                "`/balance` — Xem số xu hiện tại\n" +
                "`/shop inventory` — Xem túi đồ của bạn", inline: false)
            .AddField("📊 Bảng xếp hạng",
                "`/leaderboard fishing` — Top ngư thủ\n" +
                "`/leaderboard chat` — Top chat\n" +
                "`/leaderboard voice` — Top voice", inline: false)
            .Build(),

        // ── Section 2: Câu cá cơ bản ─────────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("🎣 Câu Cá — Hướng Dẫn Cơ Bản")
            .WithColor(new Color(0x3498DB))
            .AddField("Bắt đầu câu",
                "`/fishing cast` — Thả cần câu\n" +
                "Cooldown mặc định **30 giây** (giảm khi có cần câu tốt hơn)", inline: false)
            .AddField("Cần câu nâng cấp",
                "Mua tại cửa hàng:\n" +
                "🪁 **Tre** — 1,000 xu | 🎣 **Bạc** — 8,000 xu\n" +
                "🏆 **Vàng** — 25,000 xu | 💎 **Kim Cương** — 70,000 xu", inline: false)
            .AddField("Mồi câu",
                "🪱 **Mồi Thường** — 400 xu (10 lần, +10% Rare)\n" +
                "🦗 **Mồi Cao Cấp** — 1,200 xu (30 lần, +10% Rare)", inline: false)
            .AddField("Độ hiếm cá",
                "⬜ Common → 🟩 Uncommon → 🟦 Rare → 🟨 Legendary → 🟥 Mythic", inline: false)
            .Build(),

        // ── Section 3: Túi cá & bán cá ───────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("🎒 Túi Cá & Bán Cá")
            .WithColor(new Color(0x2ECC71))
            .AddField("Xem túi cá", "`/bag view` — Xem cá trong túi\n`/bag sell-all` — Bán tất cả cá", inline: false)
            .AddField("Nâng cấp túi",
                "🎒 **Mở Rộng** — 5,000 xu (30 slot)\n" +
                "🧳 **Siêu To** — 15,000 xu (50 slot)\n" +
                "Mua tại cửa hàng: `/shop buy Túi Cá Mở Rộng`", inline: false)
            .AddField("Bảo tàng cá",
                "`/fishing trophy` — Xem tủ trưng bày cá Rare+ của bạn\n" +
                "Cá Rare trở lên được lưu vĩnh viễn vào tủ khi bán!", inline: false)
            .Build(),

        // ── Section 4: Thời tiết ─────────────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("🌤️ Thời Tiết Bể Cá")
            .WithColor(new Color(0x9B59B6))
            .WithDescription("Thời tiết ảnh hưởng tỉ lệ câu được cá hiếm. Đổi tự động **mỗi 2 tiếng**.")
            .AddField("☀️ Nắng", "Tỉ lệ bình thường", inline: true)
            .AddField("🌧️ Mưa", "+15% Rare\n+5% Legendary", inline: true)
            .AddField("⛈️ Giông Bão", "+5% Legendary\n20% đứt cước", inline: true)
            .AddField("Kích hoạt thủ công",
                "🌧️ **Phép Triệu Mưa** — 2,500 xu\n" +
                "Kích hoạt thời tiết Mưa trong **60 phút**\n" +
                "Dùng: `/item use Phép Triệu Mưa`", inline: false)
            .Build(),

        // ── Section 5: Special Pool ───────────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("🌊 Special Pool — Hồ Câu Đặc Biệt")
            .WithColor(new Color(0x1F8EF1))
            .WithDescription("Hồ câu đặc biệt xuất hiện **mỗi tối từ 20:00 đến 05:00** (giờ Việt Nam).")
            .AddField("📅 Lịch mở cửa",
                "Tuần chẵn → mở các **ngày chẵn** trong tháng\n" +
                "Tuần lẻ → mở các **ngày lẻ** trong tháng\n" +
                "→ Xem thông báo pool trong kênh 📢 pool", inline: false)
            .AddField("✅ Điều kiện vào",
                "• **Fishing Level 20+** (`/level` để xem cấp độ)\n" +
                "• Sở hữu **🎟️ Vé Pool Đặc Biệt** (3,000 xu)\n" +
                "  1 vé = 1 lần câu trong pool", inline: false)
            .AddField("🐟 Lợi ích",
                "Drop rate cao hơn bể thường\n" +
                "Xuất hiện cá độc quyền chỉ có trong pool", inline: false)
            .AddField("Cách tham gia", "`/fishing pool cast` — Khi pool đang mở", inline: false)
            .Build(),

        // ── Section 6: Auto Câu Cá ───────────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("🤖 Auto Câu Cá")
            .WithColor(new Color(0xE74C3C))
            .WithDescription("Bot tự động câu cá cho bạn trong khi bạn offline!")
            .AddField("Mua Auto Fish",
                "`/shop buy Auto Fish 1H` — Mua gói tự động câu\n" +
                "Bot sẽ câu liên tục và kết quả hiện trong kênh câu cá", inline: false)
            .AddField("User mode", "Bán hết cá tự động + hiển thị kết quả mỗi lần câu", inline: true)
            .AddField("Xem trạng thái", "`/autofishing status` — Xem còn bao nhiêu thời gian", inline: false)
            .Build(),

        // ── Section 7: Level ─────────────────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("⭐ Hệ Thống Level")
            .WithColor(new Color(0xF39C12))
            .AddField("Xem level", "`/level` — Xem level của bạn\n`/level top` — Bảng xếp hạng level", inline: false)
            .AddField("Tích lũy XP",
                "• Mỗi lần câu được cá → +XP\n" +
                "• Cá càng hiếm → XP càng nhiều\n" +
                "• Chat và voice cũng tích lũy hoạt động server", inline: false)
            .AddField("Mục tiêu", "**Level 20** — Mở khoá quyền vào Special Pool!", inline: false)
            .WithFooter("Chúc bạn câu được nhiều cá hiếm! 🎣")
            .Build(),
    ];
}
