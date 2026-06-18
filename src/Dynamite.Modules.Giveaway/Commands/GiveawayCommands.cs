// src/Dynamite.Modules.Giveaway/Commands/GiveawayCommands.cs
namespace Dynamite.Modules.Giveaway.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Modules.Giveaway.Services;
using Microsoft.Extensions.Logging;

[Group("giveaway", "Giveaway management commands")]
[DefaultMemberPermissions(GuildPermission.ManageGuild)]
public class GiveawayCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GiveawayService _service;
    private readonly ILogger<GiveawayCommands> _logger;

    public GiveawayCommands(GiveawayService service, ILogger<GiveawayCommands> logger)
    {
        _service = service;
        _logger = logger;
    }

    [SlashCommand("start", "Start a new giveaway")]
    public async Task StartAsync(
        [Summary("prize", "What are you giving away?")] string prize,
        [Summary("duration", "Duration (e.g. 1h, 30m, 1d)")] string duration,
        [Summary("winners", "Number of winners")] int winners = 1,
        [Summary("channel", "Channel to host giveaway (default: current)")] ITextChannel? channel = null,
        [Summary("description", "Optional description")] string? description = null,
        [Summary("ping_role", "Role to ping when the giveaway is posted")] IRole? pingRole = null,
        [Summary("min_days", "Require members to be in the server at least N days (0 = anyone)")]
        [MinValue(0)] [MaxValue(3650)] int minDays = 0,
        [Summary("claim_message", "Custom DM sent to winners with claim instructions")]
        [MaxLength(1024)] string? claimMessage = null,
        [Summary("joined_before", "Only members who joined BEFORE this date (dd/MM/yyyy)")]
        string? joinedBefore = null)
    {
        await DeferAsync(ephemeral: true);

        if (!TryParseDuration(duration, out var span))
        {
            await FollowupAsync("❌ Invalid duration format. Use: `1d`, `2h`, `30m`, `1h30m`", ephemeral: true);
            return;
        }

        if (span < TimeSpan.FromMinutes(1) || span > TimeSpan.FromDays(30))
        {
            await FollowupAsync("❌ Duration must be between 1 minute and 30 days.", ephemeral: true);
            return;
        }

        if (winners < 1 || winners > 20)
        {
            await FollowupAsync("❌ Winner count must be between 1 and 20.", ephemeral: true);
            return;
        }

        // Parse mốc ngày join (hỗ trợ dd/MM/yyyy và yyyy-MM-dd)
        DateTime? joinedBeforeDate = null;
        if (!string.IsNullOrWhiteSpace(joinedBefore))
        {
            string[] formats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];
            if (!DateTime.TryParseExact(joinedBefore.Trim(), formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
            {
                await FollowupAsync(
                    "❌ Invalid `joined_before` date. Use format `dd/MM/yyyy` (e.g. `07/06/2026`).",
                    ephemeral: true);
                return;
            }
            // Npgsql yêu cầu DateTime Kind=Utc cho cột timestamptz
            joinedBeforeDate = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        var targetChannel = channel ?? (ITextChannel)Context.Channel;

        try
        {
            var giveaway = await _service.CreateAsync(
                Context.Guild.Id, targetChannel.Id, Context.User.Id,
                prize, description, winners, span,
                pingRole?.Id, minDays, claimMessage, joinedBeforeDate);

            await FollowupAsync(
                $"✅ Giveaway started in {targetChannel.Mention}! Ends <t:{new DateTimeOffset(giveaway.EndsAt).ToUnixTimeSeconds()}:R>",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create giveaway");
            await FollowupAsync("❌ Failed to start giveaway. Please try again.", ephemeral: true);
        }
    }

    // Server Owner only — pick tối đa 5 người cùng lúc
    // Giveaway vẫn chạy bình thường, người tham gia không biết
    [SlashCommand("pick", "Pre-select up to 5 winners at once (Server Owner only)")]
    public async Task PickAsync(
        [Summary("giveaway-id", "Giveaway ID (from /giveaway list)")] string giveawayIdStr,
        [Summary("user1", "Winner #1")] IGuildUser user1,
        [Summary("user2", "Winner #2")] IGuildUser? user2 = null,
        [Summary("user3", "Winner #3")] IGuildUser? user3 = null,
        [Summary("user4", "Winner #4")] IGuildUser? user4 = null,
        [Summary("user5", "Winner #5")] IGuildUser? user5 = null)
    {
        await DeferAsync(ephemeral: true);

        if (Context.Guild.OwnerId != Context.User.Id)
        {
            await FollowupAsync("❌ Only the Server Owner can pre-select giveaway winners.", ephemeral: true);
            return;
        }

        if (!Guid.TryParse(giveawayIdStr, out var giveawayId))
        {
            await FollowupAsync("❌ Invalid giveaway ID.", ephemeral: true);
            return;
        }

        var users = new[] { user1, user2, user3, user4, user5 }
            .Where(u => u is not null)
            .Select(u => u!.Id)
            .Distinct()
            .ToList();

        var results = new List<string>();
        foreach (var uid in users)
        {
            var r = await _service.PreSelectWinnerAsync(
                giveawayId, uid, Context.User.Id, Context.Guild.Id);
            results.Add(r ? $"✅ {r.Value}" : $"❌ {r.ErrorMessage}");
        }

        await FollowupAsync(string.Join("\n", results), ephemeral: true);
    }

    // Server Owner only — remove 1 người hoặc clear toàn bộ
    [SlashCommand("unpick", "Remove a pre-selected winner (omit user to clear all)")]
    public async Task UnpickAsync(
        [Summary("giveaway-id", "Giveaway ID")] string giveawayIdStr,
        [Summary("user", "User to remove (omit to clear all pre-selections)")] IGuildUser? user = null)
    {
        await DeferAsync(ephemeral: true);

        if (Context.Guild.OwnerId != Context.User.Id)
        {
            await FollowupAsync("❌ Only the Server Owner can modify giveaway winner selection.", ephemeral: true);
            return;
        }

        if (!Guid.TryParse(giveawayIdStr, out var giveawayId))
        {
            await FollowupAsync("❌ Invalid giveaway ID.", ephemeral: true);
            return;
        }

        var result = await _service.ClearPreSelectionAsync(
            giveawayId, Context.User.Id, Context.Guild.Id, user?.Id);

        await FollowupAsync(
            result
                ? $"✅ {(user is null ? "All pre-selections cleared. Giveaway will pick random winners." : $"Removed {user.Mention} from pre-selected list.")}"
                : $"❌ {result.ErrorMessage}",
            ephemeral: true);
    }

    [SlashCommand("list", "List all active giveaways in this server")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);

        var giveaways = await _service.ListActiveAsync(Context.Guild.Id);

        if (giveaways.Count == 0)
        {
            await FollowupAsync("📭 No active giveaways in this server.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("🎉 Active Giveaways")
            .WithColor(Color.Gold)
            .WithFooter($"{giveaways.Count} giveaway(s) active");

        foreach (var g in giveaways)
        {
            var endsAt = new DateTimeOffset(g.EndsAt).ToUnixTimeSeconds();
            var entryCount = g.Entries?.Count ?? 0;
            embed.AddField(
                $"🏆 {g.Prize}",
                $"**ID:** `{g.Id}`\n" +
                $"**Ends:** <t:{endsAt}:R>\n" +
                $"**Winners:** {g.WinnerCount} | **Entries:** {entryCount}\n" +
                $"**Channel:** <#{g.ChannelId}>",
                inline: false);
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("end", "End a giveaway early and announce winners now")]
    public async Task EndEarlyAsync(
        [Summary("giveaway-id", "Giveaway ID (from /giveaway list)")] string giveawayIdStr)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(giveawayIdStr, out var giveawayId))
        {
            await FollowupAsync("❌ Invalid giveaway ID.", ephemeral: true);
            return;
        }

        var result = await _service.EndEarlyAsync(giveawayId, Context.User.Id, Context.Guild.Id);
        await FollowupAsync(
            result ? "✅ Giveaway ended early — winners announced!" : $"❌ {result.ErrorMessage}",
            ephemeral: true);
    }

    [SlashCommand("cancel", "Cancel an active giveaway")]
    public async Task CancelAsync(
        [Summary("giveaway-id", "Giveaway ID (from /giveaway list)")] string giveawayIdStr)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(giveawayIdStr, out var giveawayId))
        {
            await FollowupAsync("❌ Invalid giveaway ID format.", ephemeral: true);
            return;
        }

        var success = await _service.CancelAsync(giveawayId, Context.User.Id);
        await FollowupAsync(success ? "✅ Giveaway cancelled." : "❌ Could not cancel — giveaway not found or already ended.", ephemeral: true);
    }

    [SlashCommand("reroll", "Reroll winners for an ended giveaway")]
    public async Task RerollAsync(
        [Summary("giveaway-id", "Giveaway ID (from /giveaway list)")] string giveawayIdStr)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(giveawayIdStr, out var giveawayId))
        {
            await FollowupAsync("❌ Invalid giveaway ID.", ephemeral: true);
            return;
        }

        var result = await _service.RerollAsync(giveawayId, Context.User.Id);
        await FollowupAsync(
            result ? "✅ Rerolled successfully." : $"❌ {result.ErrorMessage}",
            ephemeral: true);
    }

    [SlashCommand("entries", "List all participants of a giveaway")]
    public async Task EntriesAsync(
        [Summary("giveaway-id", "Giveaway ID (from /giveaway list)")] string giveawayIdStr)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(giveawayIdStr, out var giveawayId))
        {
            await FollowupAsync("❌ Invalid giveaway ID.", ephemeral: true);
            return;
        }

        var giveaway = await _service.GetByIdAsync(giveawayId);
        if (giveaway is null || giveaway.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("❌ Giveaway not found.", ephemeral: true);
            return;
        }

        var entries = await _service.GetEntriesAsync(giveawayId);

        if (entries.Count == 0)
        {
            await FollowupAsync("📭 No participants yet.", ephemeral: true);
            return;
        }

        // Discord embed field max 1024 chars — chunk nếu nhiều người
        const int chunkSize = 30;
        var mentions = entries.Select(e => $"<@{e.UserId}>").ToList();
        var pages = mentions.Chunk(chunkSize).ToList();

        var embed = new EmbedBuilder()
            .WithTitle($"👥 Participants — {giveaway.Prize}")
            .WithColor(Color.Blue)
            .WithFooter($"{entries.Count} participant(s)");

        for (int i = 0; i < pages.Count; i++)
            embed.AddField(pages.Count > 1 ? $"Page {i + 1}" : "Participants",
                string.Join(" ", pages[i]), inline: false);

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    // ── Duration parser ───────────────────────────────────────────────────────

    private static bool TryParseDuration(string input, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        input = input.Trim().ToLowerInvariant();

        var total = TimeSpan.Zero;
        var buffer = string.Empty;
        var matched = false;

        foreach (var ch in input)
        {
            if (char.IsDigit(ch))
            {
                buffer += ch;
            }
            else if (ch is 'd' or 'h' or 'm' or 's')
            {
                if (string.IsNullOrEmpty(buffer)) return false;
                var val = int.Parse(buffer);
                buffer = string.Empty;
                matched = true;

                total += ch switch
                {
                    'd' => TimeSpan.FromDays(val),
                    'h' => TimeSpan.FromHours(val),
                    'm' => TimeSpan.FromMinutes(val),
                    's' => TimeSpan.FromSeconds(val),
                    _ => TimeSpan.Zero
                };
            }
            else return false;
        }

        if (!matched || total == TimeSpan.Zero) return false;
        result = total;
        return true;
    }
}