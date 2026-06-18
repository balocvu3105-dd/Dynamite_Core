// src/Dynamite.Modules.Economy/Commands/AdminFishingCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Core.Common;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// /admin-fishing — Admin commands cho fishing system.
/// Tách riêng thay vì nested dưới /admin vì Discord.Net không hỗ trợ nested groups.
/// </summary>
[RequireContext(ContextType.Guild)]
[DefaultMemberPermissions(GuildPermission.Administrator)]
[RequireUserPermission(GuildPermission.Administrator)]
[Group("admin-fishing", "Admin: Quản lý dữ liệu câu cá")]
public class AdminFishingCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly FishingSnapshotService   _snapshot;
    private readonly IUserProfileRepository   _profileRepo;
    private readonly IFishBagRepository       _bagRepo;
    private readonly IFishingLogRepository    _fishLog;
    private readonly IGuildConfigRepository   _configRepo;
    private readonly ISpecialPoolRepository   _poolRepo;
    private readonly PondService              _pond;
    private readonly WeatherService           _weather;
    private readonly WeatherForecastService   _weatherForecast;
    private readonly DiscordSocketClient      _discord;
    private readonly ILogger<AdminFishingCommands> _logger;

    public AdminFishingCommands(
        FishingSnapshotService        snapshot,
        IUserProfileRepository        profileRepo,
        IFishBagRepository            bagRepo,
        IFishingLogRepository         fishLog,
        IGuildConfigRepository        configRepo,
        ISpecialPoolRepository        poolRepo,
        PondService                   pond,
        WeatherService                weather,
        WeatherForecastService        weatherForecast,
        DiscordSocketClient           discord,
        ILogger<AdminFishingCommands> logger)
    {
        _snapshot        = snapshot;
        _profileRepo     = profileRepo;
        _bagRepo         = bagRepo;
        _fishLog         = fishLog;
        _configRepo      = configRepo;
        _poolRepo        = poolRepo;
        _pond            = pond;
        _weather         = weather;
        _weatherForecast = weatherForecast;
        _discord         = discord;
        _logger          = logger;
    }

    // ── /admin-fishing set-channel ────────────────────────────────────────────

    [SlashCommand("set-channel", "Đặt channel câu cá — chỉ channel này mới dùng được lệnh câu cá")]
    public async Task SetFishingChannelAsync(
        [Summary("channel", "Channel câu cá dành riêng")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);
        config.FishingChannelId = channel.Id;
        await _configRepo.SaveChangesAsync();

        await FollowupAsync(
            $"✅ Đã set {channel.Mention} làm channel câu cá.\n" +
            $"Tất cả lệnh `/fishing`, `/bag`, `/fish-auto` chỉ hoạt động trong channel đó.",
            ephemeral: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /admin-fishing refill-pond [amount]
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("refill-pond", "Nạp lại bể cá ngay lập tức (không ảnh hưởng chu kỳ reset tự động)")]
    public async Task RefillPondAsync(
        [Summary("amount", "Số cá muốn nạp (mặc định = MaxFish, tối đa = MaxFish)")]
        [MinValue(1)]
        int? amount = null)
    {
        await DeferAsync(ephemeral: true);

        // Lấy trạng thái trước khi refill để hiển thị diff
        var before = await _pond.GetStatusAsync(Context.Guild.Id);

        PondStatus after;
        if (amount.HasValue)
        {
            // Partial refill: chỉ cộng thêm, không vượt MaxFish, không clear timer
            after = await _pond.AdminPartialRefillAsync(Context.Guild.Id, amount.Value);
        }
        else
        {
            // Full refill: CurrentFish = MaxFish + clear timer
            after = await _pond.AdminRefillAsync(Context.Guild.Id);
        }

        _logger.LogWarning(
            "[Pond] Admin {AdminId} refilled pond in guild {GuildId}: {Before} → {After}",
            Context.User.Id, Context.Guild.Id, before.CurrentFish, after.CurrentFish);

        var wasDepletedStr = before.IsEmpty
            ? $"\n⚠️ Bể đang **trống** — timer reset đã bị hủy."
            : before.ResetAvailableAt.HasValue
                ? $"\n⏳ Đang đếm ngược reset — timer đã bị hủy."
                : "";

        var embed = new EmbedBuilder()
            .WithColor(new Color(0x57F287))
            .WithTitle("🪣 Bể Cá Đã Được Nạp Lại")
            .WithDescription(
                $"**Trước:** {before.CurrentFish:N0} / {before.MaxFish:N0} con\n" +
                $"**Sau:** {after.CurrentFish:N0} / {after.MaxFish:N0} con" +
                wasDepletedStr + "\n\n" +
                $"Chu kỳ reset tự động (30 phút khi hết) **không bị ảnh hưởng**.\n" +
                $"Khi bể cạn lần tiếp theo, bộ đếm 30 phút sẽ chạy bình thường.")
            .WithFooter($"Admin: {Context.User.Username}")
            .WithCurrentTimestamp()
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /admin-fishing set-fishing-role <role>
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("set-fishing-role", "Đặt role được mention khi thời tiết bể cá thay đổi")]
    public async Task SetFishingRoleAsync(
        [Summary("role", "Role muốn mention (vd: @Ngư Dân)")] IRole role)
    {
        await DeferAsync(ephemeral: true);

        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);
        config.FishingRoleId = role.Id;
        await _configRepo.SaveChangesAsync();

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0x2ECC71))
                .WithTitle("✅ Đã Đặt Role Thông Báo")
                .WithDescription($"Role {role.Mention} sẽ được mention mỗi khi thời tiết bể cá thay đổi.")
                .Build(),
            ephemeral: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /admin-fishing set-weather <weather> [minutes]
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("set-weather", "Ép thời tiết bể cá — tự động quay về chu kỳ bình thường sau khi hết giờ")]
    public async Task SetWeatherAsync(
        [Summary("weather", "Loại thời tiết muốn set")] PondWeather weather,
        [Summary("minutes", "Thời gian giữ (phút, mặc định 120 = 2 tiếng, tối đa 480)")]
        [MinValue(1)][MaxValue(480)]
        int minutes = 120)
    {
        await DeferAsync(ephemeral: true);

        var before = await _weather.GetCurrentWeatherAsync(Context.Guild.Id);
        await _weather.ForceWeatherAsync(Context.Guild.Id, weather, minutes);

        _logger.LogInformation(
            "[Weather] Admin {AdminId} forced {Weather} for {Minutes}m in guild {GuildId}",
            Context.User.Id, weather, minutes, Context.Guild.Id);

        var expiresAt   = DateTime.UtcNow.AddMinutes(minutes);
        var expiresUnix = ((DateTimeOffset)expiresAt).ToUnixTimeSeconds();

        var (rareMod, legendaryMod, _, _, _) = WeatherService.GetModifiers(weather);
        var effectDesc = weather switch
        {
            PondWeather.Rainy  => $"🌧️ Rare **+{rareMod * 100:0}%** · Legendary **+{legendaryMod * 100:0}%**",
            PondWeather.Stormy => $"⛈️ Legendary **+{legendaryMod * 100:0}%** · Miss rate **+8%**",
            _                  => "☀️ Tỷ lệ bình thường (không buff/debuff)"
        };

        var embed = new EmbedBuilder()
            .WithColor(WeatherColor(weather))
            .WithTitle($"{WeatherService.GetWeatherEmoji(weather)} Thời Tiết Đã Được Thay Đổi")
            .AddField("Trước", $"{WeatherService.GetWeatherEmoji(before)} {before}", inline: true)
            .AddField("Sau", $"{WeatherService.GetWeatherEmoji(weather)} {weather}", inline: true)
            .AddField("Thời gian giữ", $"<t:{expiresUnix}:R> (<t:{expiresUnix}:T>)", inline: false)
            .AddField("Hiệu ứng", effectDesc, inline: false)
            .WithDescription("Sau khi hết thời gian, chu kỳ xoay vòng 2 tiếng tự động sẽ tiếp tục bình thường.")
            .WithFooter($"Admin: {Context.User.Username}")
            .WithCurrentTimestamp()
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);

        // Refresh embed dự báo thời tiết sau khi force
        await _weatherForecast.RefreshAsync(Context.Guild.Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /admin-fishing reset @user [confirm]
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("reset", "⚠️ XÓA TOÀN BỘ dữ liệu câu cá của user (không thể hoàn tác)")]
    public async Task ResetAsync(
        [Summary("user", "User cần reset")] SocketGuildUser target,
        [Summary("confirm", "Gõ 'CONFIRM' để xác nhận — thao tác KHÔNG THỂ HOÀN TÁC")]
        string confirm)
    {
        await DeferAsync(ephemeral: true);

        if (confirm != "CONFIRM")
        {
            await FollowupAsync(
                embed: ErrorEmbed("Xác nhận sai",
                    "Bạn phải nhập chính xác **CONFIRM** vào ô `confirm` để thực hiện reset."),
                ephemeral: true);
            return;
        }

        try
        {
            // 1. Tạo snapshot backup trước khi xóa (safety net)
            await _snapshot.CreateSnapshotAsync(Context.Guild.Id, target.Id, "pre-reset-by-admin");

            // 2. Xóa fishing profile → EF cascade sẽ xóa achievements
            var profile = await _profileRepo.GetOrCreateFishingAsync(Context.Guild.Id, target.Id);
            ResetFishingProfile(profile);

            // 3. Xóa fish bag
            var bag = await _bagRepo.GetOrCreateAsync(Context.Guild.Id, target.Id);
            if (bag.Fish.Count > 0)
                await _bagRepo.RemoveFishAsync(bag.Fish.ToList());
            bag.BagCapacity = 10; // reset về default

            await _profileRepo.SaveChangesAsync();
            await _bagRepo.SaveChangesAsync();

            _logger.LogWarning(
                "Fishing data RESET for user {UserId} in guild {GuildId} by admin {AdminId}",
                target.Id, Context.Guild.Id, Context.User.Id);

            await FollowupAsync(
                embed: SuccessEmbed("✅ Reset thành công",
                    $"Dữ liệu câu cá của {target.Mention} đã được reset.\n" +
                    "Snapshot backup tự động đã được tạo trước khi reset (dùng `/admin-fishing restore` nếu cần)."),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset fishing data for user {UserId}", target.Id);
            await FollowupAsync(
                embed: ErrorEmbed("Lỗi", $"Reset thất bại: `{ex.Message}`"),
                ephemeral: true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /admin-fishing backup @user [reason]
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("backup", "Tạo snapshot thủ công cho dữ liệu câu cá của user")]
    public async Task BackupAsync(
        [Summary("user", "User cần backup")] SocketGuildUser target,
        [Summary("reason", "Lý do backup (ví dụ: trước khi test)")] string reason = "manual")
    {
        await DeferAsync(ephemeral: true);

        try
        {
            var snap = await _snapshot.CreateSnapshotAsync(
                Context.Guild.Id, target.Id, $"manual:{reason}");

            await FollowupAsync(
                embed: SuccessEmbed("💾 Backup thành công",
                    $"Đã tạo snapshot cho {target.Mention}.\n" +
                    $"**ID:** `{snap.Id:N}`\n" +
                    $"**Lý do:** {reason}\n" +
                    $"**Thời gian:** <t:{new DateTimeOffset(snap.CreatedAt).ToUnixTimeSeconds()}:f>"),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup fishing data for user {UserId}", target.Id);
            await FollowupAsync(
                embed: ErrorEmbed("Lỗi", $"Backup thất bại: `{ex.Message}`"),
                ephemeral: true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /admin-fishing snapshots @user
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("snapshots", "Xem danh sách snapshot có thể restore cho user")]
    public async Task ListSnapshotsAsync(
        [Summary("user", "User cần xem snapshots")] SocketGuildUser target)
    {
        await DeferAsync(ephemeral: true);

        var snaps = await _snapshot.GetUserSnapshotsAsync(Context.Guild.Id, target.Id);

        if (snaps.Count == 0)
        {
            await FollowupAsync(
                embed: InfoEmbed("Không có snapshot",
                    $"{target.Mention} chưa có snapshot nào. Dùng `/admin-fishing backup` để tạo."),
                ephemeral: true);
            return;
        }

        var builder = new EmbedBuilder()
            .WithTitle($"📦 Snapshots — {target.DisplayName}")
            .WithDescription($"Có **{snaps.Count}** snapshot (tối đa 5). Dùng ID với `/admin-fishing restore`.")
            .WithColor(new Color(0x5865F2))
            .WithTimestamp(DateTimeOffset.UtcNow);

        foreach (var (s, i) in snaps.Select((s, i) => (s, i + 1)))
        {
            var timeStr = $"<t:{new DateTimeOffset(s.CreatedAt).ToUnixTimeSeconds()}:f>";
            builder.AddField(
                $"#{i} — {s.Reason}",
                $"**Thời gian:** {timeStr}\n" +
                $"**Level:** {s.FishingLevel} · **Tổng câu:** {s.TotalCaught} · **Xu:** {s.WalletCoins:N0}\n" +
                $"**ID:** `{s.Id:N}`");
        }

        await FollowupAsync(embed: builder.Build(), ephemeral: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /admin-fishing restore @user <snapshot-id>
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("restore", "Restore dữ liệu câu cá từ snapshot")]
    public async Task RestoreAsync(
        [Summary("user", "User cần restore")] SocketGuildUser target,
        [Summary("snapshot-id", "GUID của snapshot (lấy từ /admin-fishing snapshots)")]
        string snapshotIdStr)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(snapshotIdStr, out var snapshotId))
        {
            await FollowupAsync(
                embed: ErrorEmbed("ID không hợp lệ",
                    "Snapshot ID phải là GUID hợp lệ (ví dụ: `550e8400-e29b-41d4-a716-446655440000`).\n" +
                    "Dùng `/admin-fishing snapshots` để xem danh sách."),
                ephemeral: true);
            return;
        }

        try
        {
            var result = await _snapshot.RestoreSnapshotAsync(
                Context.Guild.Id, target.Id, snapshotId);

            var embed = result
                ? SuccessEmbed("✅ Restore thành công", $"{target.Mention} — {result.Value}")
                : ErrorEmbed("Restore thất bại", result.ErrorMessage);

            if (result)
                _logger.LogWarning(
                    "Fishing data RESTORED for user {UserId} in guild {GuildId} from snapshot {SnapshotId} by admin {AdminId}",
                    target.Id, Context.Guild.Id, snapshotId, Context.User.Id);

            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore fishing data for user {UserId}", target.Id);
            await FollowupAsync(
                embed: ErrorEmbed("Lỗi", $"Restore thất bại: `{ex.Message}`"),
                ephemeral: true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /admin-fishing log [@user] [type] [limit]
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("log", "Xem FishingActivityLog (Miss, Escape, Caught, v.v.)")]
    public async Task FishLogAsync(
        [Summary("user", "Lọc theo user (để trống = toàn guild)")] SocketGuildUser? target = null,
        [Summary("type", "Lọc theo loại sự kiện")] FishingEvent? eventType = null,
        [Summary("limit", "Số dòng tối đa (mặc định 15, tối đa 25)")]
        [MinValue(1)][MaxValue(25)] int limit = 15)
    {
        await DeferAsync(ephemeral: true);

        List<FishingActivityLog> logs;
        if (target is not null)
            logs = await _fishLog.GetUserLogsAsync(Context.Guild.Id, target.Id, limit, eventType);
        else
            logs = await _fishLog.GetGuildLogsAsync(Context.Guild.Id, limit, eventType);

        if (logs.Count == 0)
        {
            await FollowupAsync(
                embed: InfoEmbed("Không có log",
                    "Không tìm thấy log nào với bộ lọc đã chọn."),
                ephemeral: true);
            return;
        }

        var targetStr   = target is not null ? $" — {target.DisplayName}" : string.Empty;
        var typeStr     = eventType.HasValue ? $" [{eventType}]" : string.Empty;

        var builder = new EmbedBuilder()
            .WithTitle($"🎣 Fishing Log{targetStr}{typeStr}")
            .WithDescription($"Hiển thị **{logs.Count}** sự kiện gần nhất")
            .WithColor(new Color(0x0099FF))
            .WithTimestamp(DateTimeOffset.UtcNow);

        foreach (var log in logs)
        {
            var timeStr   = $"<t:{new DateTimeOffset(log.CreatedAt).ToUnixTimeSeconds()}:R>";
            var eventIcon = EventEmoji(log.Event);
            var details   = new System.Text.StringBuilder();

            if (log.FishName is not null)
                details.Append($"🐟 {log.FishName}");
            if (log.Rarity is not null)
                details.Append($" [{log.Rarity}]");
            if (log.CoinsEarned > 0)
                details.Append($" · 🪙{log.CoinsEarned:N0}");
            if (log.XpEarned > 0)
                details.Append($" · ⭐{log.XpEarned}XP");
            if (log.RodName is not null)
                details.Append($" · 🎣{log.RodName}");
            if (log.PoolName is not null)
                details.Append($" · 🌊{log.PoolName}");

            var detailsStr = details.Length > 0 ? $"\n{details}" : string.Empty;

            builder.AddField(
                $"{eventIcon} {log.Event} — <@{log.UserId}>",
                $"{timeStr}{detailsStr}");
        }

        await FollowupAsync(embed: builder.Build(), ephemeral: true);
    }

    // ── /admin-fishing view-bag ──────────────────────────────────────────────

    [SlashCommand("view-bag", "Xem túi cá của một user bất kỳ (Admin)")]
    public async Task ViewBagAsync(
        [Summary("user", "User cần xem túi cá")] IUser target)
    {
        await DeferAsync(ephemeral: true);

        var bag = await _bagRepo.GetOrCreateAsync(Context.Guild.Id, target.Id);

        var embed = EconomyEmbedBuilder.BuildBagEmbed(bag,
            (target as IGuildUser)?.DisplayName ?? target.Username);

        // Override footer để admin biết đây là view của người khác
        var embedBuilder = embed.ToEmbedBuilder()
            .WithFooter($"👁️ Xem bởi Admin | User: {target.Username} ({target.Id})");

        await FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
    }

    // ── /admin-fishing open ──────────────────────────────────────────────────

    [SlashCommand("open", "Mở bãi câu — cho phép user dùng lệnh câu cá trở lại")]
    public async Task OpenFishingAsync(
        [Summary("announce", "Thông báo lý do vào channel câu cá (tùy chọn)")] string? message = null)
    {
        await DeferAsync(ephemeral: true);

        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);

        if (config.FishingEnabled)
        {
            await FollowupAsync("ℹ️ Bãi câu đang **mở** rồi — không cần làm gì thêm.", ephemeral: true);
            return;
        }

        config.FishingEnabled = true;
        await _configRepo.SaveChangesAsync();

        _logger.LogInformation("[AdminFishing] Admin {AdminId} OPENED fishing area in guild {GuildId}",
            Context.User.Id, Context.Guild.Id);

        // Thông báo nội bộ cho admin
        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0x57F287))
                .WithTitle("✅ Bãi Câu Đã Mở!")
                .WithDescription("Tất cả lệnh câu cá hoạt động trở lại bình thường.")
                .WithFooter($"Admin: {Context.User.Username}")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);

        // Thông báo public vào channel câu cá (nếu có)
        if (config.FishingChannelId.HasValue &&
            _discord.GetChannel(config.FishingChannelId.Value) is IMessageChannel fishChannel)
        {
            var desc = message is not null
                ? $"**Lý do:** {message}"
                : "Bãi câu đã sẵn sàng cho mùa câu mới!";

            var announcement = new EmbedBuilder()
                .WithColor(new Color(0x57F287))
                .WithTitle("🎣 Bãi Câu Đã Mở Cửa!")
                .WithDescription(desc + "\n\nDùng `/fishing cast` để bắt đầu câu cá nhé!")
                .WithFooter("Dynamite Fishing System")
                .WithCurrentTimestamp()
                .Build();

            await fishChannel.SendMessageAsync(
                config.FishingRoleId.HasValue ? $"<@&{config.FishingRoleId}>" : null,
                embed: announcement);
        }
    }

    // ── /admin-fishing close ─────────────────────────────────────────────────

    [SlashCommand("close", "Đóng bãi câu — chặn toàn bộ lệnh câu cá (auto-fish vẫn giữ timer)")]
    public async Task CloseFishingAsync(
        [Summary("reason", "Lý do đóng cửa (sẽ hiển thị trong thông báo)")] string? reason = null,
        [Summary("stop-auto", "Dừng luôn toàn bộ session auto-fish?")] bool stopAuto = false)
    {
        await DeferAsync(ephemeral: true);

        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);

        if (!config.FishingEnabled)
        {
            await FollowupAsync("ℹ️ Bãi câu đang **đóng** rồi — không cần làm gì thêm.", ephemeral: true);
            return;
        }

        config.FishingEnabled = false;
        await _configRepo.SaveChangesAsync();

        _logger.LogWarning("[AdminFishing] Admin {AdminId} CLOSED fishing area in guild {GuildId}. Reason: {Reason}",
            Context.User.Id, Context.Guild.Id, reason ?? "—");

        // Dừng auto-fish nếu admin chọn
        int stoppedCount = 0;
        if (stopAuto)
        {
            var profiles = await _profileRepo.GetAllActiveAutoFishProfilesAsync();
            var guildProfiles = profiles.Where(p => p.GuildId == Context.Guild.Id).ToList();
            foreach (var p in guildProfiles)
            {
                p.AutoFishExpiresAt            = DateTime.UtcNow;
                p.AutoFishPaused               = false;
                p.AutoFishSpecialPoolId        = null;
                p.AutoFishSpecialPoolExpiresAt = null;
            }
            if (guildProfiles.Count > 0)
                await _profileRepo.SaveChangesAsync();
            stoppedCount = guildProfiles.Count;
        }

        // Thông báo nội bộ cho admin
        var stopLine = stopAuto
            ? $"\n⛔ Đã dừng **{stoppedCount}** session auto-fish."
            : "\n⚠️ Session auto-fish vẫn giữ timer — bot sẽ bỏ qua tick cho đến khi mở lại.";

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0xE74C3C))
                .WithTitle("🔒 Bãi Câu Đã Đóng Cửa")
                .WithDescription($"Toàn bộ lệnh câu cá đã bị chặn.{stopLine}")
                .WithFooter($"Admin: {Context.User.Username}")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);

        // Thông báo public vào channel câu cá (nếu có)
        if (config.FishingChannelId.HasValue &&
            _discord.GetChannel(config.FishingChannelId.Value) is IMessageChannel fishChannel)
        {
            var reasonLine = reason is not null ? $"**Lý do:** {reason}\n\n" : string.Empty;

            var announcement = new EmbedBuilder()
                .WithColor(new Color(0xE74C3C))
                .WithTitle("🔒 Bãi Câu Tạm Đóng Cửa")
                .WithDescription(
                    reasonLine +
                    "Bãi câu cá hiện **không khả dụng**.\n" +
                    "Vui lòng đợi thông báo mở lại từ Admin.")
                .WithFooter("Dynamite Fishing System")
                .WithCurrentTimestamp()
                .Build();

            await fishChannel.SendMessageAsync(
                config.FishingRoleId.HasValue ? $"<@&{config.FishingRoleId}>" : null,
                embed: announcement);
        }
    }

    // ── /admin-fishing stop-all-auto ────────────────────────────────────────

    [SlashCommand("stop-all-auto", "Dừng toàn bộ session auto-fish của tất cả user trong guild")]
    public async Task StopAllAutoAsync()
    {
        await DeferAsync(ephemeral: true);

        var profiles = await _profileRepo.GetAllActiveAutoFishProfilesAsync();
        var guildProfiles = profiles.Where(p => p.GuildId == Context.Guild.Id).ToList();

        if (guildProfiles.Count == 0)
        {
            await FollowupAsync("ℹ️ Không có session auto-fish nào đang chạy.", ephemeral: true);
            return;
        }

        foreach (var p in guildProfiles)
        {
            p.AutoFishExpiresAt              = DateTime.UtcNow;
            p.AutoFishSellAll                = false;
            p.AutoFishPaused                 = false;
            p.AutoFishSpecialPoolId          = null;
            p.AutoFishSpecialPoolExpiresAt   = null;
        }

        await _profileRepo.SaveChangesAsync();

        _logger.LogWarning(
            "[AdminFishing] Admin {AdminId} force-stopped {Count} auto-fish session(s) in guild {GuildId}",
            Context.User.Id, guildProfiles.Count, Context.Guild.Id);

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0xE74C3C))
                .WithTitle("⛔ Đã Dừng Toàn Bộ Auto-Fish")
                .WithDescription(
                    $"**{guildProfiles.Count}** session auto-fish đã bị dừng.\n\n" +
                    string.Join("\n", guildProfiles.Select(p => $"• <@{p.UserId}>")))
                .WithFooter($"Thực hiện bởi {Context.User.Username}")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static void ResetFishingProfile(Core.Entities.UserFishingProfile p)
    {
        p.FishingXp          = 0;
        p.FishingLevel       = 0;
        p.TotalCaught        = 0;
        p.CommonCaught       = 0;
        p.UncommonCaught     = 0;
        p.RareCaught         = 0;
        p.LegendaryCaught    = 0;
        p.MythicCaught       = 0;
        p.ChestsOpened       = 0;
        p.TradesThisWeek     = 0;
        p.TradeWeekResetAt   = null;
        p.AutoFishExpiresAt              = null;
        p.AutoFishPaused                 = false;
        p.AutoFishSpecialPoolId          = null;
        p.AutoFishSpecialPoolExpiresAt   = null;
        p.LastFishedAt                   = null;
        p.Achievements.Clear();
    }

    private static Color WeatherColor(PondWeather w) => w switch
    {
        PondWeather.Rainy  => new Color(0x3498DB), // xanh dương
        PondWeather.Stormy => new Color(0x9B59B6), // tím
        _                  => new Color(0xF1C40F)  // vàng (Sunny)
    };

    private static string EventEmoji(FishingEvent e) => e switch
    {
        FishingEvent.Caught         => "✅",
        FishingEvent.Miss           => "❌",
        FishingEvent.Escape         => "💨",
        FishingEvent.BagFull        => "🎒",
        FishingEvent.PearlCaught    => "🔮",
        FishingEvent.PearlCapHit    => "🚫",
        FishingEvent.StormBreak     => "⛈️",
        FishingEvent.SpecialCaught  => "⭐",
        FishingEvent.SpecialEscape  => "💫",
        _                           => "📌"
    };

    private static Embed SuccessEmbed(string title, string desc)
        => new EmbedBuilder().WithTitle(title).WithDescription(desc)
            .WithColor(new Color(0x57F287)).WithTimestamp(DateTimeOffset.UtcNow).Build();

    private static Embed ErrorEmbed(string title, string desc)
        => new EmbedBuilder().WithTitle(title).WithDescription(desc)
            .WithColor(new Color(0xED4245)).WithTimestamp(DateTimeOffset.UtcNow).Build();

    private static Embed InfoEmbed(string title, string desc)
        => new EmbedBuilder().WithTitle(title).WithDescription(desc)
            .WithColor(new Color(0x5865F2)).WithTimestamp(DateTimeOffset.UtcNow).Build();

    // ── /admin-fishing spawn-pool ─────────────────────────────────────────────

    [SlashCommand("spawn-pool", "Mở pool đặc biệt ngay lập tức (Admin)")]
    public async Task SpawnPoolAsync(
        [Summary("type", "Loại pool")]
        [Choice("Vịnh San Hô 🪸",       "CoralBay")]
        [Choice("Đáy Đại Dương 🌊",      "DeepOcean")]
        [Choice("Rừng Ngập Mặn 🌿",      "MangroveForest")]
        [Choice("Vực Thẳm Huyền Bí 🌑", "AbyssalZone")]
        string type,
        [Summary("duration", "Thời gian mở (giờ, mặc định 2h)")]
        [MinValue(1)][MaxValue(24)]
        int durationHours = 2,
        [Summary("min-level", "Level tối thiểu (mặc định 20)")]
        [MinValue(1)][MaxValue(50)]
        int minLevel = 20,
        [Summary("capacity", "Số cá trong pool (mặc định 2000)")]
        [MinValue(100)][MaxValue(10000)]
        int capacity = 2000)
    {
        await DeferAsync(ephemeral: true);

        if (!Enum.TryParse<SpecialDropTable>(type, out var dropTable))
        {
            await FollowupAsync("❌ Loại pool không hợp lệ.", ephemeral: true);
            return;
        }

        var now       = DateTime.UtcNow;
        var expiresAt = now.AddHours(durationHours);

        var poolName = dropTable switch
        {
            SpecialDropTable.CoralBay        => "Vịnh San Hô 🪸",
            SpecialDropTable.DeepOcean       => "Đáy Đại Dương 🌊",
            SpecialDropTable.MangroveForest  => "Rừng Ngập Mặn 🌿",
            SpecialDropTable.AbyssalZone     => "Vực Thẳm Huyền Bí 🌑",
            _                               => type
        };

        var pool = new SpecialPool
        {
            GuildId       = Context.Guild.Id,
            PoolName      = poolName,
            DropTable     = dropTable,
            Capacity      = capacity,
            RemainingFish = capacity,
            MinLevel      = minLevel,
            StartsAt      = now,
            ExpiresAt     = expiresAt,
            CreatedAt     = now
        };

        await _poolRepo.AddPoolAsync(pool);
        await _poolRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[AdminFishing] {AdminId} spawned pool {Name} ({Type}) for {Hours}h in guild {GuildId}",
            Context.User.Id, poolName, dropTable, durationHours, Context.Guild.Id);

        // Thông báo vào SpecialPoolChannelId nếu có
        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);
        var announceChannelId = config.SpecialPoolChannelId ?? config.FishingChannelId;
        if (announceChannelId.HasValue)
        {
            var ch = _discord.GetGuild(Context.Guild.Id)?.GetTextChannel(announceChannelId.Value);
            if (ch is not null)
            {
                var expiresUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
                await ch.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(new Color(0xF39C12))
                    .WithTitle($"🎣 Pool Đặc Biệt Xuất Hiện!")
                    .WithDescription(
                        $"**{poolName}** vừa được Admin mở!\n\n" +
                        $"Level tối thiểu: **{minLevel}+**\n" +
                        $"Cá trong pool: **{capacity:N0}**\n" +
                        $"Đóng lúc: <t:{expiresUnix}:F> (<t:{expiresUnix}:R>)\n\n" +
                        $"Dùng `/fishing pools` để xem và `/fishing pool-cast` để câu!")
                    .WithFooter($"Mở bởi Admin {Context.User.Username}")
                    .WithCurrentTimestamp()
                    .Build());
            }
        }

        var closeUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0x57F287))
                .WithTitle("✅ Pool Đặc Biệt Đã Mở")
                .AddField("Pool",      poolName,                      inline: true)
                .AddField("Duration",  $"{durationHours}h",           inline: true)
                .AddField("Min Level", $"Level {minLevel}+",          inline: true)
                .AddField("Capacity",  $"{capacity:N0} cá",           inline: true)
                .AddField("Đóng lúc", $"<t:{closeUnix}:R>",          inline: true)
                .WithFooter("Dùng /fishing pools để user xem pool")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);
    }
}
