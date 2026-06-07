// src/Dynamite.Modules/Security/SecurityEventHandler.cs
namespace Dynamite.Modules.Security;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Security.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class SecurityEventHandler
{
    private readonly DiscordSocketClient _client;
    private readonly ViolationTracker _tracker;
    private readonly EscalationEngine _escalation;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityEventHandler> _logger;

    // Invite regex pattern
    private static readonly System.Text.RegularExpressions.Regex InviteRegex =
        new(@"discord(?:\.gg|app\.com\/invite|\.com\/invite)\/[a-zA-Z0-9\-]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Compiled);

    // URL regex pattern
    private static readonly System.Text.RegularExpressions.Regex UrlRegex =
        new(@"https?://[^\s]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Compiled);

    public SecurityEventHandler(
        DiscordSocketClient client,
        ViolationTracker tracker,
        EscalationEngine escalation,
        IServiceScopeFactory scopeFactory,
        ILogger<SecurityEventHandler> logger)
    {
        _client = client;
        _tracker = tracker;
        _escalation = escalation;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Subscribe()
    {
        _client.MessageReceived += msg => SafeRun(() => OnMessageReceivedAsync(msg));
        _client.UserJoined += user => SafeRun(() => OnUserJoinedAsync(user));
        _logger.LogInformation("SecurityEventHandler subscribed");
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        // Bỏ qua bot và system messages
        if (message.Author.IsBot) return;
        if (message is not SocketUserMessage userMessage) return;
        if (message.Author is not SocketGuildUser guildUser) return;
        if (message.Channel is not SocketTextChannel) return;

        // Bỏ qua admins và moderators
        if (guildUser.GuildPermissions.Administrator) return;
        if (guildUser.GuildPermissions.ManageMessages) return;

        // Load config từ DB
        using var scope = _scopeFactory.CreateScope();
        var antiSpamService = scope.ServiceProvider.GetRequiredService<IAntiSpamService>();
        var config = await antiSpamService.GetConfigAsync(guildUser.Guild.Id);

        // AntiSpam chưa được config hoặc bị disable
        if (config is null || !config.Enabled) return;

        // ── Spam check ───────────────────────────────────
        var messageCount = _tracker.RecordMessage(
            guildUser.Guild.Id, guildUser.Id, config.MessageWindowSeconds);

        if (messageCount >= config.MessageThreshold)
        {
            await _escalation.HandleViolationAsync(message, "spam");
            return;
        }

        // ── Mention spam check ───────────────────────────
        var mentionCount = message.MentionedUsers.Count
                         + message.MentionedRoles.Count;

        if (mentionCount >= config.MentionThreshold)
        {
            await _escalation.HandleViolationAsync(message, "mention-spam");
            return;
        }

        // ── Anti-invite check ────────────────────────────
        if (config.AntiInvite && InviteRegex.IsMatch(message.Content))
        {
            try { await message.DeleteAsync(); } catch { }
            await SendTemporaryNoticeAsync(
                (ITextChannel)message.Channel,
                SecurityEmbeds.InviteBlocked(guildUser.DisplayName));
            return;
        }

        // ── Anti-scam link check ─────────────────────────
        if (config.AntiScamLink)
        {
            var urls = UrlRegex.Matches(message.Content)
                .Select(m => m.Value)
                .ToList();

            foreach (var url in urls)
            {
                if (ScamDomains.IsScamLink(url))
                {
                    await _escalation.HandleViolationAsync(message, "scam-link");
                    return;
                }
            }
        }
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        using var scope = _scopeFactory.CreateScope();
        var antiSpamService = scope.ServiceProvider.GetRequiredService<IAntiSpamService>();
        var config = await antiSpamService.GetConfigAsync(user.Guild.Id);

        if (config is null || !config.Enabled || !config.AntiRaid) return;

        var joinCount = _tracker.RecordJoin(user.Guild.Id);

        if (joinCount >= config.RaidThreshold)
        {
            _logger.LogWarning("Raid detected in guild {GuildId} — {Count} joins in 10s",
                user.Guild.Id, joinCount);

            await AlertModsAsync(user.Guild, joinCount);
        }
    }

    private async Task AlertModsAsync(SocketGuild guild, int joinCount)
    {
        using var scope = _scopeFactory.CreateScope();
        var guildConfigService = scope.ServiceProvider.GetRequiredService<IGuildConfigService>();

        var config = await guildConfigService.GetOrCreateConfigAsync(
            guild.Id, guild.Name);

        if (config.ModLogChannelId is null) return;

        var channel = guild.GetTextChannel(config.ModLogChannelId.Value);
        if (channel is null) return;

        var embed = SecurityEmbeds.RaidAlert(joinCount, 10);
        await channel.SendMessageAsync(
            text: "@here Possible raid detected!",
            embed: embed);
    }

    private static async Task SendTemporaryNoticeAsync(ITextChannel channel, Embed embed)
    {
        try
        {
            var msg = await channel.SendMessageAsync(embed: embed);
            _ = Task.Delay(TimeSpan.FromSeconds(5))
                .ContinueWith(_ => msg.DeleteAsync());
        }
        catch { /* non-critical */ }
    }

    private Task SafeRun(Func<Task> handler)
    {
        _ = Task.Run(async () =>
        {
            try { await handler(); }
            catch (Exception ex)
            { _logger.LogError(ex, "Unhandled exception in SecurityEventHandler"); }
        });
        return Task.CompletedTask;
    }
}