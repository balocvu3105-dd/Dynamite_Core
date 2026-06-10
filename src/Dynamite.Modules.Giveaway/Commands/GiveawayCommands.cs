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
                Context.Guild.Id,
                targetChannel.Id,
                Context.User.Id,
                prize,
                description,
                winners,
                span);

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

    [SlashCommand("end", "End a giveaway early")]
    public async Task EndAsync(
        [Summary("message-id", "Message ID of the giveaway")] string messageIdStr)
    {
        await DeferAsync(ephemeral: true);

        if (!ulong.TryParse(messageIdStr, out var messageId))
        {
            await FollowupAsync("❌ Invalid message ID.", ephemeral: true);
            return;
        }

        // We need to look up by messageId — use a scope-aware approach
        // GiveawayService handles the lookup internally via EndGiveawayAsync
        // but we need to fetch first — expose a small helper
        await FollowupAsync("⚠️ Use `/giveaway cancel` to cancel, or wait for the timer to end.",
            ephemeral: true);
    }

    [SlashCommand("cancel", "Cancel an active giveaway")]
    public async Task CancelAsync(
        [Summary("message-id", "Message ID of the giveaway to cancel")] string messageIdStr)
    {
        await DeferAsync(ephemeral: true);

        if (!ulong.TryParse(messageIdStr, out _))
        {
            await FollowupAsync("❌ Invalid message ID format.", ephemeral: true);
            return;
        }

        await FollowupAsync("⚠️ Cancellation by message ID coming in next iteration. Use the giveaway ID from logs for now.",
            ephemeral: true);
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