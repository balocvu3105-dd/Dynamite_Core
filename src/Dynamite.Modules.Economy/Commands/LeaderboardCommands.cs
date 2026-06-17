// src/Dynamite.Modules.Economy/Commands/LeaderboardCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;

[Group("leaderboard", "Bảng xếp hạng server")]
public class LeaderboardCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILeaderboardRepository _lbRepo;
    private readonly IFishTrophyRepository _trophyRepo;

    public LeaderboardCommands(
        ILeaderboardRepository lbRepo,
        IFishTrophyRepository trophyRepo)
    {
        _lbRepo = lbRepo;
        _trophyRepo = trophyRepo;
    }

    // ── /leaderboard fishing ──────────────────────────────────────────────────

    [SlashCommand("fishing", "Top ngư thủ trong tuần")]
    public async Task FishingAsync()
        => await ShowLeaderboard(LeaderboardType.Fishing);

    // ── /leaderboard chat ─────────────────────────────────────────────────────

    [SlashCommand("chat", "Top chat trong tuần")]
    public async Task ChatAsync()
        => await ShowLeaderboard(LeaderboardType.Chat);

    // ── /leaderboard voice ────────────────────────────────────────────────────

    [SlashCommand("voice", "Top voice trong tuần")]
    public async Task VoiceAsync()
        => await ShowLeaderboard(LeaderboardType.Voice);

    // ── /leaderboard collector ────────────────────────────────────────────────

    [SlashCommand("collector", "Top bộ sưu tập cá Rare+ độc nhất")]
    public async Task CollectorAsync()
    {
        await DeferAsync();

        var excluded = GetPrivilegedIds(Context.Guild);
        var top = (await _trophyRepo.GetTopCollectorsAsync(Context.Guild.Id, top: 15))
            .Where(t => !excluded.Contains(t.UserId))
            .Take(10)
            .ToList();

        if (top.Count == 0)
        {
            await FollowupAsync(
                "🏆 Chưa có ai thu thập được cá **Rare+** trong server này!",
                ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithColor(new Color(0xF1C40F))
            .WithTitle("🏆 Bảng Xếp Hạng Collector")
            .WithDescription("Top người sở hữu nhiều **loài cá Rare+** độc nhất")
            .WithCurrentTimestamp();

        var medals = new[] { "🥇", "🥈", "🥉" };

        for (int i = 0; i < top.Count; i++)
        {
            var (userId, uniqueCount) = top[i];
            var user = Context.Guild.GetUser(userId);
            var displayName = user is not null ? user.DisplayName : $"<@{userId}>";
            var medal = i < medals.Length ? medals[i] : $"**#{i + 1}**";
            embed.AddField($"{medal} {displayName}", $"🐟 **{uniqueCount}** loài độc nhất", inline: false);
        }

        embed.WithFooter("Chỉ tính cá Rare, Legendary, Mythic • Admin/Owner không hiển thị");

        await FollowupAsync(embed: embed.Build());
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    private async Task ShowLeaderboard(LeaderboardType type)
    {
        var snapshot = await _lbRepo.GetLatestSnapshotAsync(Context.Guild.Id, type);

        if (snapshot is null || snapshot.Entries.Count == 0)
        {
            await RespondAsync(
                "📊 Chưa có dữ liệu bảng xếp hạng. Snapshot đầu tiên sẽ có vào Chủ Nhật 12:00 UTC.",
                ephemeral: true);
            return;
        }

        var weekLabel = snapshot.WeekStartDate.ToString("dd/MM/yyyy");
        var excluded  = GetPrivilegedIds(Context.Guild);
        var embed = EconomyEmbedBuilder.BuildLeaderboardEmbed(
            snapshot,
            Context.Guild,
            weekLabel,
            excluded);

        await RespondAsync(embed: embed);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về tập UserId của owner + tất cả member có GuildPermission.Administrator.
    /// Những người này không hiển thị trên bảng xếp hạng.
    /// </summary>
    private static HashSet<ulong> GetPrivilegedIds(SocketGuild guild)
    {
        var ids = new HashSet<ulong> { guild.OwnerId };

        foreach (var member in guild.Users)
        {
            if (member.GuildPermissions.Administrator)
                ids.Add(member.Id);
        }

        return ids;
    }
}
