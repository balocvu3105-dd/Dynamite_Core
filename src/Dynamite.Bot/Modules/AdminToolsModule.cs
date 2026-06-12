// src/Dynamite.Bot/Modules/AdminToolsModule.cs
//
// Công cụ admin: /announce, /say, /reply
// Cho phép staff đăng thông báo / nói chuyện qua danh nghĩa bot.
// QUAN TRỌNG: mọi lần dùng đều ghi audit log đầy đủ (ai dùng, nội dung gì)
// — ẩn danh với member, KHÔNG ẩn danh với hệ thống.
//
// Đặt tạm trong Dynamite.Bot vì là utility gắn với bot host.
// Nếu sau này nở thêm tính năng → tách thành Dynamite.Modules.Utility.
namespace Dynamite.Bot.Modules;

using Discord;
using Discord.Interactions;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.ManageGuild)]
[DefaultMemberPermissions(GuildPermission.ManageGuild)]
public class AdminToolsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminToolsModule> _logger;

    public AdminToolsModule(
        IServiceScopeFactory scopeFactory,
        ILogger<AdminToolsModule> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ── /announce ─────────────────────────────────────────────────────────────
    // Mở modal để dán nội dung dài, đăng vào channel chỉ định + tag role chỉ định

    [SlashCommand("announce", "Post an announcement as the bot (opens an editor)")]
    public async Task AnnounceAsync(
        [Summary("channel", "Channel to post the announcement in")] ITextChannel channel,
        [Summary("ping_role", "Role to ping with the announcement")] IRole? pingRole = null)
    {
        // channelId + roleId truyền qua custom_id (0 = không ping)
        var customId = $"admin_announce_{channel.Id}_{pingRole?.Id ?? 0}";
        await RespondWithModalAsync<AnnounceModal>(customId);
    }

    [ModalInteraction("admin_announce_*_*")]
    public async Task OnAnnounceModalAsync(string channelIdStr, string roleIdStr, AnnounceModal modal)
    {
        await DeferAsync(ephemeral: true);

        if (!ulong.TryParse(channelIdStr, out var channelId))
        {
            await FollowupAsync("❌ Invalid channel.", ephemeral: true);
            return;
        }

        var channel = Context.Guild.GetTextChannel(channelId);
        if (channel is null)
        {
            await FollowupAsync("❌ Channel no longer exists.", ephemeral: true);
            return;
        }

        ulong.TryParse(roleIdStr, out var roleId);

        var embed = new EmbedBuilder()
            .WithTitle(modal.AnnounceTitle)
            .WithDescription(modal.Content)
            .WithColor(new Color(0x5865F2))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        // Mention phải nằm trong content (mention trong embed không ping)
        string? content = null;
        AllowedMentions? mentions = null;
        if (roleId != 0)
        {
            content = $"<@&{roleId}>";
            mentions = new AllowedMentions { RoleIds = [roleId] };
        }

        try
        {
            await channel.SendMessageAsync(text: content, embed: embed, allowedMentions: mentions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post announcement to channel {ChannelId}", channelId);
            await FollowupAsync("❌ Could not post — check my permissions in that channel.", ephemeral: true);
            return;
        }

        await SendAuditAsync("📢 Announcement posted",
            $"**By:** {Context.User.Mention} (`{Context.User.Id}`)\n" +
            $"**Channel:** {channel.Mention}\n" +
            $"**Ping role:** {(roleId != 0 ? $"<@&{roleId}>" : "none")}\n" +
            $"**Title:** {modal.AnnounceTitle}\n" +
            $"**Content:** {Truncate(modal.Content, 800)}");

        await FollowupAsync($"✅ Announcement posted in {channel.Mention}.", ephemeral: true);
    }

    // ── /say ──────────────────────────────────────────────────────────────────

    [SlashCommand("say", "Send a message as the bot")]
    public async Task SayAsync(
        [Summary("message", "What the bot should say")] string message,
        [Summary("channel", "Target channel (default: current)")] ITextChannel? channel = null)
    {
        await DeferAsync(ephemeral: true);

        var target = channel ?? (ITextChannel)Context.Channel;

        try
        {
            // Chỉ cho ping user — chặn @everyone/@role qua bot
            await target.SendMessageAsync(message,
                allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed /say to channel {ChannelId}", target.Id);
            await FollowupAsync("❌ Could not send — check my permissions in that channel.", ephemeral: true);
            return;
        }

        await SendAuditAsync("💬 /say used",
            $"**By:** {Context.User.Mention} (`{Context.User.Id}`)\n" +
            $"**Channel:** {target.Mention}\n" +
            $"**Content:** {Truncate(message, 800)}");

        await FollowupAsync($"✅ Sent in {target.Mention}.", ephemeral: true);
    }

    // ── /reply ────────────────────────────────────────────────────────────────

    [SlashCommand("reply", "Reply to a specific message as the bot")]
    public async Task ReplyAsync(
        [Summary("message_link", "Right-click the message → Copy Message Link")] string messageLink,
        [Summary("message", "The reply content")] string message)
    {
        await DeferAsync(ephemeral: true);

        // Link format: https://discord.com/channels/{guildId}/{channelId}/{messageId}
        if (!TryParseMessageLink(messageLink, out var guildId, out var channelId, out var messageId)
            || guildId != Context.Guild.Id)
        {
            await FollowupAsync(
                "❌ Invalid message link. Right-click the target message → **Copy Message Link**.",
                ephemeral: true);
            return;
        }

        var channel = Context.Guild.GetTextChannel(channelId);
        if (channel is null)
        {
            await FollowupAsync("❌ Channel not found.", ephemeral: true);
            return;
        }

        try
        {
            await channel.SendMessageAsync(message,
                messageReference: new MessageReference(messageId, channelId, guildId),
                allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed /reply in channel {ChannelId}", channelId);
            await FollowupAsync("❌ Could not reply — the message may have been deleted.", ephemeral: true);
            return;
        }

        await SendAuditAsync("↩️ /reply used",
            $"**By:** {Context.User.Mention} (`{Context.User.Id}`)\n" +
            $"**Channel:** {channel.Mention}\n" +
            $"**Replied to:** {messageLink}\n" +
            $"**Content:** {Truncate(message, 800)}");

        await FollowupAsync($"✅ Replied in {channel.Mention}.", ephemeral: true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SendAuditAsync(string title, string description)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();
            var auditChannelId = await logService.GetLogChannelAsync(Context.Guild.Id, LogCategory.Audit);
            if (auditChannelId is null) return;

            var channel = Context.Guild.GetTextChannel(auditChannelId.Value);
            if (channel is null) return;

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(new Color(0x99AAB5))
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write admin-tools audit log");
        }
    }

    private static bool TryParseMessageLink(
        string link, out ulong guildId, out ulong channelId, out ulong messageId)
    {
        guildId = channelId = messageId = 0;

        var marker = "/channels/";
        var idx = link.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;

        var parts = link[(idx + marker.Length)..]
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return false;

        return ulong.TryParse(parts[0], out guildId)
            && ulong.TryParse(parts[1], out channelId)
            && ulong.TryParse(parts[2].Split('?')[0], out messageId);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";
}

// Modal nhập nội dung announcement — hỗ trợ nội dung dài nhiều dòng
public class AnnounceModal : IModal
{
    public string Title => "Post Announcement";

    [InputLabel("Title")]
    [ModalTextInput("announce_title", TextInputStyle.Short,
        placeholder: "📢 Thông báo từ Cộng Hội", maxLength: 256)]
    public string AnnounceTitle { get; set; } = string.Empty;

    [InputLabel("Content")]
    [ModalTextInput("announce_content", TextInputStyle.Paragraph,
        placeholder: "Nội dung thông báo...", maxLength: 4000)]
    public string Content { get; set; } = string.Empty;
}
