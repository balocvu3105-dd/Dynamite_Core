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
    private readonly ILeaderboardRepository  _lbRepo;
    private readonly IFishTrophyRepository   _trophyRepo;
    private readonly IGuildConfigRepository  _configRepo;

    public LeaderboardCommands(
        ILeaderboardRepository lbRepo,
        IFishTrophyRepository  trophyRepo,
        IGuildConfigRepository configRepo)
    {
        _lbRepo     = lbRepo;
        _trophyRepo = trophyRepo;
        _configRepo = configRepo;
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
        await DeferAsync(ephemeral: true);

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
            var user        = Context.Guild.GetUser(userId);
            var displayName = user is not null ? user.DisplayName : $"<@{userId}>";
            var medal       = i < medals.Length ? medals[i] : $"**#{i + 1}**";
            embed.AddField($"{medal} {displayName}", $"🐟 **{uniqueCount}** loài độc nhất", inline: false);
        }

        embed.WithFooter("Chỉ tính cá Rare, Legendary, Mythic • Admin/Owner không hiển thị");

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    // ── /leaderboard set-fishing-board ───────────────────────────────────────

    [SlashCommand("set-fishing-board", "Đặt channel auto-post bảng ngư dân mỗi Chủ Nhật (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetFishingBoardAsync(
        [Summary("channel", "Channel nhận bảng xếp hạng ngư dân (Fishing + Collector)")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);
        config.FishingLeaderboardChannelId = channel.Id;
        await _configRepo.SaveChangesAsync();

        await FollowupAsync(
            $"✅ Bảng xếp hạng **Ngư Dân** sẽ được post vào {channel.Mention} mỗi Chủ Nhật 12:00 UTC.",
            ephemeral: true);
    }

    // ── /leaderboard set-server-board ────────────────────────────────────────

    [SlashCommand("set-server-board", "Đặt channel auto-post bảng server mỗi Chủ Nhật (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetServerBoardAsync(
        [Summary("channel", "Channel nhận bảng xếp hạng server (Chat + Voice)")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);
        config.ServerLeaderboardChannelId = channel.Id;
        await _configRepo.SaveChangesAsync();

        await FollowupAsync(
            $"✅ Bảng xếp hạng **Server** sẽ được post vào {channel.Mention} mỗi Chủ Nhật 12:00 UTC.",
            ephemeral: true);
    }

    // ── /leaderboard set-pool-channel ────────────────────────────────────────

    [SlashCommand("set-pool-channel", "Đặt channel thông báo khi Pool Đặc Biệt xuất hiện (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetPoolChannelAsync(
        [Summary("channel", "Channel nhận thông báo Pool Đặc Biệt (20:00–05:00 VN)")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var config = await _configRepo.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);
        config.SpecialPoolChannelId = channel.Id;
        await _configRepo.SaveChangesAsync();

        await FollowupAsync(
            $"✅ Thông báo **Pool Đặc Biệt** sẽ được gửi vào {channel.Mention} lúc 20:00 VN (các ngày hợp lệ).",
            ephemeral: true);
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    private async Task ShowLeaderboard(LeaderboardType type)
    {
        await DeferAsync(ephemeral: true);

        var snapshot = await _lbRepo.GetLatestSnapshotAsync(Context.Guild.Id, type);

        if (snapshot is null || snapshot.Entries.Count == 0)
        {
            await FollowupAsync(
                "📊 Chưa có dữ liệu bảng xếp hạng. Snapshot đầu tiên sẽ có vào Chủ Nhật 12:00 UTC.",
                ephemeral: true);
            return;
        }

        var weekLabel = snapshot.WeekStartDate.ToString("dd/MM/yyyy");
        var excluded  = GetPrivilegedIds(Context.Guild);
        var embed     = EconomyEmbedBuilder.BuildLeaderboardEmbed(
            snapshot, Context.Guild, weekLabel, excluded);

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static HashSet<ulong> GetPrivilegedIds(SocketGuild guild)
    {
        var ids = new HashSet<ulong> { guild.OwnerId };
        foreach (var member in guild.Users)
            if (member.GuildPermissions.Administrator)
                ids.Add(member.Id);
        return ids;
    }
}
