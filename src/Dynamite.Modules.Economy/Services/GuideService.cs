// src/Dynamite.Modules.Economy/Services/GuideService.cs
namespace Dynamite.Modules.Economy.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Common;
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
    public async Task<ServiceResult> SetChannelAsync(
        ulong guildId, string guildName, ITextChannel channel)
    {
        var config = await _configRepo.GetOrCreateAsync(guildId, guildName);
        config.GuideChannelId = channel.Id;
        await _configRepo.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    /// <summary>
    /// Đăng tất cả embed hướng dẫn vào channel đã cấu hình.
    /// Trả về số embed đã đăng khi thành công.
    /// </summary>
    public async Task<ServiceResult<int>> PostGuideAsync(ulong guildId)
    {
        var config = await _configRepo.GetByGuildIdAsync(guildId);
        if (config?.GuideChannelId is null)
            return ServiceResult<int>.Fail("Chưa đặt kênh cẩm nang. Dùng `/guide set-channel` trước.");

        var guild   = _discord.GetGuild(guildId);
        var channel = guild?.GetTextChannel(config.GuideChannelId.Value);
        if (channel is null)
            return ServiceResult<int>.Fail("Không tìm thấy channel. Kiểm tra lại `/guide set-channel`.");

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

            var total = sections.Count + 1;
            _logger.LogInformation("[Guide] Posted {Count} embeds to guild {GuildId}", total, guildId);
            return ServiceResult<int>.Ok(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Guide] Failed to post for guild {GuildId}", guildId);
            return ServiceResult<int>.Fail($"Lỗi khi đăng hướng dẫn: `{ex.Message}`");
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
                "`/fishing pond` — Xem trạng thái bể + thời tiết hiện tại", inline: false)
            .AddField("Cần câu nâng cấp",
                "🎋 **Tân Thủ** — miễn phí | 25s CD | ×1.0 *(không gãy)*\n" +
                "Mua tại `/shop buy <tên cần>`:\n" +
                "🪁 **Tre** — 3,000 xu | 22s CD | ×1.1 | độ bền 200\n" +
                "🎣 **Bạc** — 20,000 xu | 19s CD | ×1.25 | độ bền 300\n" +
                "🏆 **Vàng** — 60,000 xu | 15s CD | ×1.55 | độ bền 600\n" +
                "💎 **Kim Cương** — 160,000 xu | 10s CD | ×2.0 | độ bền 1000 | 🍀 May Mắn +1", inline: false)
            .AddField("🔧 Độ bền & Sửa cần",
                "Cần câu mòn dần sau mỗi lần câu. Khi **gãy (0)** không thể câu tiếp.\n" +
                "• `/shop repair-rod` — Sửa cần, chi phí = **50% giá mua** × % hao mòn\n" +
                "• `/shop repair-rod preview:True` — Xem chi phí trước khi sửa\n" +
                "• Autocomplete tự gợi ý cần hỏng nhất của bạn\n" +
                "💡 **May Mắn +1** (Kim Cương): mỗi điểm → Rare +10% · Legendary +15%", inline: false)
            .AddField("Mồi câu",
                "🪱 **Mồi Thường** — 400 xu (10 lần dùng, +10% Rare)\n" +
                "🦗 **Mồi Cao Cấp** — 1,200 xu (30 lần dùng, +10% Rare)", inline: false)
            .AddField("Độ hiếm cá",
                "⬜ Common → 🟩 Uncommon → 🟦 Rare → 🟨 Legendary → 🟥 Mythic", inline: false)
            .Build(),

        // ── Section 3: Túi cá & bán cá ───────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("🎒 Túi Cá & Bán Cá")
            .WithColor(new Color(0x2ECC71))
            .AddField("Xem & bán cá",
                "`/bag view` — Xem cá trong túi\n" +
                "`/bag sell-all` — Bán tất cả cá\n" +
                "`/bag sell-rarity <độ hiếm>` — Bán theo độ hiếm", inline: false)
            .AddField("Nâng cấp túi (+10 slot mỗi lần)",
                "Túi mặc định **10 slot** (miễn phí). Mua tại `/shop buy Nâng Túi Cá +10`:\n" +
                "10→20: **10,000** xu\n" +
                "20→30: **20,000** xu\n" +
                "30→40: **35,000** xu\n" +
                "40→50: **55,000** xu\n" +
                "50→60: **80,000** xu\n" +
                "60→70: **110,000** xu\n" +
                "70→80: **145,000** xu\n" +
                "80→90: **185,000** xu\n" +
                "90→100: **230,000** xu\n" +
                "⚠️ Tối đa **100 slot** — giá hiển thị đúng trong `/shop view`", inline: false)
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
            .AddField("🌧️ Mưa", "+15% Rare · +5% Legendary\n−10% tỉ lệ hụt cần", inline: true)
            .AddField("⛈️ Giông Bão", "+5% Legendary · ×1.25 giá bán\n+8% tỉ lệ hụt cần · 20% đứt cước", inline: true)
            .AddField("Kích hoạt thủ công",
                "🌧️ **Phép Triệu Mưa** — 20,000 xu\n" +
                "Kích hoạt thời tiết Mưa trong **60 phút** (buff drop rate + giảm hụt cần)\n" +
                "Dùng: `/shop use Phép Triệu Mưa`", inline: false)
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
                "• Sở hữu **🎟️ Vé Pool Đặc Biệt** — **15,000 xu**\n" +
                "  **1 vé = 2 tiếng câu pool đặc biệt** (không giới hạn số lần câu trong 2h)", inline: false)
            .AddField("🤖 Auto + Pool",
                "Kích hoạt vé khi dùng `/fish-auto set-mode special-pool <pool_id>`\n" +
                "Bot sẽ câu pool đặc biệt trong 2 tiếng, sau đó tự chuyển về bể thường", inline: false)
            .AddField("🐟 Lợi ích",
                "Drop rate cao hơn bể thường\n" +
                "Xuất hiện cá độc quyền chỉ có trong pool", inline: false)
            .AddField("Câu thủ công",
                "`/fishing pools` — Xem danh sách pool đang mở (lấy Pool ID)\n" +
                "`/fishing pool-cast pool-id:<ID>` — Câu trong pool đặc biệt\n" +
                "Bot tự check vé — nếu chưa có session sẽ tiêu **1 vé** và mở **2 tiếng** câu",
                inline: false)
            .Build(),

        // ── Section 6: Auto Câu Cá ───────────────────────────────────────────
        new EmbedBuilder()
            .WithTitle("🤖 Auto Câu Cá")
            .WithColor(new Color(0xE74C3C))
            .WithDescription("Bot tự động câu cá cho bạn trong khi bạn offline!\nKết quả luôn hiển thị trong kênh câu cá.")
            .AddField("🛒 Mua gói",
                "`/fish-auto buy` — Mua / gia hạn gói **5 tiếng** (giá leo thang theo lần mua)\n" +
                "Lần 1: **10,000** xu · Lần 2: **24,000** · Lần 3: **50,000** · Lần 4: **90,000** · Lần 5+: **140,000**",
                inline: false)
            .AddField("🎯 Chọn chế độ câu",
                "`/fish-auto set-mode regular` — Câu bể thường\n" +
                "`/fish-auto set-mode special-pool <pool_id>` — Câu pool đặc biệt (cần có vé, 2 tiếng)",
                inline: false)
            .AddField("⏸️ Tạm dừng / Tiếp tục",
                "`/fish-auto pause` — Tạm dừng (timer vẫn chạy)\n" +
                "`/fish-auto resume` — Bật lại sau khi tạm dừng",
                inline: false)
            .AddField("⛔ Dừng & Kiểm tra",
                "`/fish-auto stop` — Dừng session (không hoàn tiền)\n" +
                "`/fish-auto status` — Xem còn bao nhiêu thời gian",
                inline: false)
            .AddField("📌 Lưu ý",
                "• Bot câu mỗi **27 giây**, cá được **lưu vào túi** (không tự bán)\n" +
                "• Khi túi đầy → bot tự **tạm dừng** và ping bạn — bán cá rồi `/fish-auto resume`\n" +
                "• Túi tối đa **100 slot** — nâng cấp tại `/shop buy Nâng Túi Cá +10`\n" +
                "• Tên bạn hiển thị trên mỗi kết quả câu",
                inline: false)
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
