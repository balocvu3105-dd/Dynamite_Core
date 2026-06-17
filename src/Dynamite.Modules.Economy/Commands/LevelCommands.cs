// src/Dynamite.Modules.Economy/Commands/LevelCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Services;

/// <summary>
/// /level — Xem cấp độ và tiến trình XP của bản thân (hoặc user khác).
///
/// Hiển thị:
///   - Server Level (từ chat + voice XP)
///   - Fishing Level (từ câu cá XP)
///   - Progress bar trực quan
///   - XP hiện tại / XP cần để lên level tiếp
/// </summary>
[RequireContext(ContextType.Guild)]
public class LevelCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IUserProfileRepository _profileRepo;

    public LevelCommands(IUserProfileRepository profileRepo)
    {
        _profileRepo = profileRepo;
    }

    [SlashCommand("level", "Xem cấp độ và tiến trình XP của bạn (hoặc người khác)")]
    public async Task LevelAsync(
        [Summary("user", "User muốn xem (để trống = bản thân)")] SocketGuildUser? target = null)
    {
        await DeferAsync(ephemeral: true);

        var subject = target ?? (SocketGuildUser)Context.User;

        var serverProfile  = await _profileRepo.GetOrCreateServerAsync(Context.Guild.Id, subject.Id);
        var fishingProfile = await _profileRepo.GetOrCreateFishingAsync(Context.Guild.Id, subject.Id);

        // ── Server XP progress ───────────────────────────────────────────────
        var serverLevel   = serverProfile.ServerLevel;
        var serverXp      = serverProfile.ServerXp;
        var serverNeeded  = XpService.XpForNextLevel(serverLevel + 1);
        var serverBar     = ProgressBar(serverXp, serverNeeded);

        // ── Fishing XP progress ──────────────────────────────────────────────
        var fishLevel  = fishingProfile.FishingLevel;
        var fishXp     = fishingProfile.FishingXp;
        var fishNeeded = XpService.XpForNextLevel(fishLevel + 1);
        var fishBar    = ProgressBar(fishXp, fishNeeded);

        var isSelf = subject.Id == Context.User.Id;
        var title  = isSelf ? "📊 Cấp độ của bạn" : $"📊 Cấp độ — {subject.DisplayName}";

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithThumbnailUrl(subject.GetDisplayAvatarUrl() ?? subject.GetDefaultAvatarUrl())
            .WithColor(new Color(0x5865F2u))
            .WithTimestamp(DateTimeOffset.UtcNow)

            // ── Server section ────────────────────────────────────────────
            .AddField("💬 Server Level",
                $"**Level {serverLevel}**\n" +
                $"{serverBar}\n" +
                $"`{serverXp:N0}` / `{serverNeeded:N0}` XP",
                inline: false)

            // ── Fishing section ───────────────────────────────────────────
            .AddField("🎣 Fishing Level",
                $"**Level {fishLevel}**\n" +
                $"{fishBar}\n" +
                $"`{fishXp:N0}` / `{fishNeeded:N0}` XP",
                inline: false)

            // ── Extra stats ───────────────────────────────────────────────
            .AddField("📈 Thống kê",
                $"🐟 Tổng câu: **{fishingProfile.TotalCaught:N0}**\n" +
                $"🏆 Mythic: **{fishingProfile.MythicCaught}**\n" +
                $"🎙️ Voice: **{serverProfile.TotalVoiceMinutes:N0}** phút",
                inline: false)

            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── Progress bar helper ───────────────────────────────────────────────────

    /// <summary>
    /// Tạo progress bar 10 ô dùng block characters.
    /// Ví dụ: [████████░░] 80%
    /// </summary>
    private static string ProgressBar(long current, long total, int width = 10)
    {
        if (total <= 0) return $"[{"█".PadRight(width, '█')}] MAX";

        var ratio  = Math.Clamp((double)current / total, 0, 1);
        var filled = (int)(ratio * width);
        var empty  = width - filled;

        var bar     = new string('█', filled) + new string('░', empty);
        var percent = (int)(ratio * 100);
        return $"[{bar}] {percent}%";
    }
}
