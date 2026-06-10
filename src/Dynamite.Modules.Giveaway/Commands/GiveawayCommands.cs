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
        [Summary("description", "Optional description")] string? description = null)
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

        var targetChannel = channel ?? (ITextChannel)Context.Channel;

        try
        {
            var giveaway = await _service.CreateAsync(
                Context.Guild.Id, targetChannel.Id, Context.User.Id,
                prize, description, winners, span);

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

    // Server Owner only — pre-select winner trước khi hết giờ
    // Giveaway vẫn chạy bình thường, người tham gia không biết
    [SlashCommand("pick", "Pre-select a winner (Server Owner only — announced when giveaway ends)")]
    public async Task PickAsync(
        [Summary("giveaway-id", "Giveaway ID (from /giveaway list)")] string giveawayIdStr,
        [Summary("user", "User to pre-select as winner")] IGuildUser user)
    {
        await DeferAsync(ephemeral: true);

        // Chỉ Server Owner mới dùng được
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

        var (success, message) = await _service.PreSelectWinnerAsync(
            giveawayId, user.Id, Context.User.Id, Context.Guild.Id);

        await FollowupAsync(success ? $"✅ {message}" : $"❌ {message}", ephemeral: true);
    }

    // Server Owner only — xóa pre-selection, trở về random
    [SlashCommand("unpick", "Clear pre-selected winner — giveaway will pick randomly")]
    public async Task UnpickAsync(
        [Summary("giveaway-id", "Giveaway ID")] string giveawayIdStr)
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

        var (success, message) = await _service.ClearPreSelectionAsync(
            giveawayId, Context.User.Id, Context.Guild.Id);

        await FollowupAsync(success ? $"✅ {message}" : $"❌ {message}", ephemeral: true);
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

        var (success, message) = await _service.RerollAsync(giveawayId, Context.User.Id);
        await FollowupAsync(success ? $"✅ {message}" : $"❌ {message}", ephemeral: true);
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