// src/Dynamite.Modules/Moderation/Modules/ModLogModule.cs
namespace Dynamite.Modules.Moderation.Modules;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Moderation.Helpers;
using Microsoft.Extensions.Logging;

/// <summary>
/// /modlog — Truy vấn lịch sử hành động moderation từ DB.
///
/// Subcommands:
///   /modlog mod @mod [limit]   — xem các lệnh DO một mod thực hiện
///   /modlog user @user [limit] — xem các lệnh ÁP DỤNG LÊN một user
///   /modlog recent [count] [type] — xem các lệnh gần nhất trong guild
///
/// Ai dùng được:
///   - /modlog mod + recent → yêu cầu ViewAuditLog (staff/admin)
///   - /modlog user → ManageMessages trở lên (mod biết lịch sử người họ xử lý)
///
/// Data source: bảng ModerationActions trong DB (ghi tự động bởi ModerationService).
/// Không dùng Discord Audit Log → không giới hạn 90 ngày và có filter tốt hơn.
/// </summary>
[RequireContext(ContextType.Guild)]
[Group("modlog", "Xem lịch sử hành động moderation")]
public class ModLogModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IModerationRepository _modRepo;
    private readonly ILogger<ModLogModule>  _logger;

    public ModLogModule(IModerationRepository modRepo, ILogger<ModLogModule> logger)
    {
        _modRepo = modRepo;
        _logger  = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /modlog mod @mod [limit]
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("mod", "Xem các hành động DO một moderator thực hiện")]
    [RequireUserPermission(GuildPermission.ViewAuditLog)]
    public async Task ModByModeratorAsync(
        [Summary("mod", "Moderator cần xem lịch sử")] SocketGuildUser mod,
        [Summary("limit", "Số lượng hành động (tối đa 25, mặc định 10)")]
        [MinValue(1)][MaxValue(25)] int limit = 10)
    {
        await DeferAsync(ephemeral: true);

        var actions = (await _modRepo.GetByModeratorAsync(
            Context.Guild.Id, mod.Id, limit)).ToList();

        if (actions.Count == 0)
        {
            await FollowupAsync(embed: EmbedHelper.Info(
                "Không có dữ liệu",
                $"{mod.Mention} chưa thực hiện hành động moderation nào được ghi nhận."),
                ephemeral: true);
            return;
        }

        var embed = BuildModActionsEmbed(
            title: $"🛡️ Lịch sử mod — {mod.DisplayName}",
            subtitle: $"Hiển thị **{actions.Count}** hành động gần nhất",
            actions: actions,
            showTarget: true);

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /modlog user @user [limit]
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("user", "Xem lịch sử hành động áp dụng lên một user")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task ModByUserAsync(
        [Summary("user", "User cần xem lịch sử")] SocketGuildUser target,
        [Summary("limit", "Số lượng hành động (tối đa 25, mặc định 10)")]
        [MinValue(1)][MaxValue(25)] int limit = 10)
    {
        await DeferAsync(ephemeral: true);

        var actions = (await _modRepo.GetUserHistoryAsync(
            Context.Guild.Id, target.Id, limit)).ToList();

        if (actions.Count == 0)
        {
            await FollowupAsync(embed: EmbedHelper.Info(
                "Không có dữ liệu",
                $"{target.Mention} chưa có hành động moderation nào trong server này."),
                ephemeral: true);
            return;
        }

        var embed = BuildModActionsEmbed(
            title: $"📋 Lịch sử — {target.DisplayName}",
            subtitle: $"Hiển thị **{actions.Count}** hành động gần nhất",
            actions: actions,
            showTarget: false); // target đã ở title

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /modlog recent [count] [type]
    // ─────────────────────────────────────────────────────────────────────────

    [SlashCommand("recent", "Xem các hành động moderation gần nhất trong server")]
    [RequireUserPermission(GuildPermission.ViewAuditLog)]
    public async Task ModRecentAsync(
        [Summary("count", "Số lượng hành động (tối đa 25, mặc định 15)")]
        [MinValue(1)][MaxValue(25)] int count = 15,
        [Summary("type", "Lọc theo loại hành động (để trống = tất cả)")]
        ModerationActionType? type = null)
    {
        await DeferAsync(ephemeral: true);

        var actions = (await _modRepo.GetRecentActionsAsync(
            Context.Guild.Id, count, type)).ToList();

        if (actions.Count == 0)
        {
            var typeLabel = type.HasValue ? $" loại **{ActionLabel(type.Value)}**" : string.Empty;
            await FollowupAsync(embed: EmbedHelper.Info(
                "Không có dữ liệu",
                $"Chưa có hành động moderation{typeLabel} nào trong server."),
                ephemeral: true);
            return;
        }

        var typeStr = type.HasValue ? $" [{ActionLabel(type.Value)}]" : string.Empty;
        var embed = BuildModActionsEmbed(
            title: $"📜 Hoạt động mod gần nhất{typeStr}",
            subtitle: $"Hiển thị **{actions.Count}** hành động mới nhất",
            actions: actions,
            showTarget: true);

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Build embed hiển thị danh sách ModerationAction.
    /// Mỗi action = 1 field (inline: false) để dễ đọc.
    /// Discord limit 25 fields/embed, nên chúng ta đã cap tại 25 ở command level.
    /// </summary>
    private static Embed BuildModActionsEmbed(
        string title, string subtitle,
        List<ModerationAction> actions, bool showTarget)
    {
        var builder = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(subtitle)
            .WithColor(new Color(0x5865F2))   // Discord blurple
            .WithTimestamp(DateTimeOffset.UtcNow)
            .WithFooter("Dữ liệu từ DB · Chỉ hiển thị với staff");

        foreach (var a in actions)
        {
            var timeStr = $"<t:{new DateTimeOffset(a.CreatedAt).ToUnixTimeSeconds()}:R>";
            var icon    = ActionEmoji(a.ActionType);
            var label   = ActionLabel(a.ActionType);

            // Line 1: icon + type + time
            // Line 2: target (nếu cần) + mod
            // Line 3: reason
            var fieldValue = showTarget
                ? $"**Target:** <@{a.TargetUserId}> · **Mod:** <@{a.ModeratorId}>\n" +
                  $"**Lý do:** {a.Reason}\n" +
                  $"{timeStr}"
                : $"**Mod:** <@{a.ModeratorId}>\n" +
                  $"**Lý do:** {a.Reason}\n" +
                  $"{timeStr}";

            if (a.ExpiresAt.HasValue)
                fieldValue += $"\n**Hết hạn:** <t:{new DateTimeOffset(a.ExpiresAt.Value).ToUnixTimeSeconds()}:f>";

            builder.AddField($"{icon} {label}", fieldValue);
        }

        return builder.Build();
    }

    private static string ActionEmoji(ModerationActionType type) => type switch
    {
        ModerationActionType.Warn      => "⚠️",
        ModerationActionType.Kick      => "👢",
        ModerationActionType.Ban       => "🔨",
        ModerationActionType.Unban     => "✅",
        ModerationActionType.Timeout   => "⏱️",
        ModerationActionType.Untimeout => "🔓",
        _                              => "📌"
    };

    private static string ActionLabel(ModerationActionType type) => type switch
    {
        ModerationActionType.Warn      => "Warn",
        ModerationActionType.Kick      => "Kick",
        ModerationActionType.Ban       => "Ban",
        ModerationActionType.Unban     => "Unban",
        ModerationActionType.Timeout   => "Timeout",
        ModerationActionType.Untimeout => "Untimeout",
        _                              => type.ToString()
    };
}
