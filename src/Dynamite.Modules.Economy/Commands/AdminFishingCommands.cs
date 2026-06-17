// src/Dynamite.Modules.Economy/Commands/AdminFishingCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// /admin-fishing — Admin commands cho fishing system.
/// Tách riêng thay vì nested dưới /admin vì Discord.Net không hỗ trợ nested groups.
/// </summary>
[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
[Group("admin-fishing", "Admin: Quản lý dữ liệu câu cá")]
public class AdminFishingCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly FishingSnapshotService   _snapshot;
    private readonly IUserProfileRepository   _profileRepo;
    private readonly IFishBagRepository       _bagRepo;
    private readonly IFishingLogRepository    _fishLog;
    private readonly IGuildConfigRepository   _configRepo;
    private readonly ILogger<AdminFishingCommands> _logger;

    public AdminFishingCommands(
        FishingSnapshotService        snapshot,
        IUserProfileRepository        profileRepo,
        IFishBagRepository            bagRepo,
        IFishingLogRepository         fishLog,
        IGuildConfigRepository        configRepo,
        ILogger<AdminFishingCommands> logger)
    {
        _snapshot    = snapshot;
        _profileRepo = profileRepo;
        _bagRepo     = bagRepo;
        _fishLog     = fishLog;
        _configRepo  = configRepo;
        _logger      = logger;
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
            var (success, message) = await _snapshot.RestoreSnapshotAsync(
                Context.Guild.Id, target.Id, snapshotId);

            var embed = success
                ? SuccessEmbed("✅ Restore thành công", $"{target.Mention} — {message}")
                : ErrorEmbed("Restore thất bại", message);

            if (success)
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
        p.AutoFishExpiresAt  = null;
        p.LastFishedAt       = null;
        p.Achievements.Clear();
    }

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
}
