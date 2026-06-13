// src/Dynamite.Modules.Giveaway/Services/GiveawayService.cs
namespace Dynamite.Modules.Giveaway.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Giveaway.Helpers;
using Dynamite.Modules.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class GiveawayService
{
    private readonly IGiveawayRepository _repo;
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GiveawayService> _logger;
    private static readonly Random _rng = new();

    public GiveawayService(
        IGiveawayRepository repo,
        DiscordSocketClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<GiveawayService> logger)
    {
        _repo = repo;
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Giveaway> CreateAsync(
        ulong guildId, ulong channelId, ulong hostId,
        string prize, string? description, int winnerCount, TimeSpan duration,
        ulong? pingRoleId = null, int minJoinDays = 0, string? claimMessage = null,
        DateTime? joinedBefore = null)
    {
        var channel = _client.GetGuild(guildId)?.GetTextChannel(channelId)
            ?? throw new InvalidOperationException("Channel not found.");

        var giveaway = new Giveaway
        {
            GuildId = guildId,
            ChannelId = channelId,
            HostId = hostId,
            Prize = prize,
            Description = description,
            WinnerCount = winnerCount,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.Add(duration),
            MessageId = 0,
            PingRoleId = pingRoleId,
            MinJoinDays = minJoinDays < 0 ? 0 : minJoinDays,
            ClaimMessage = string.IsNullOrWhiteSpace(claimMessage) ? null : claimMessage,
            JoinedBefore = joinedBefore
        };

        var embed = GiveawayEmbedBuilder.BuildActiveEmbed(giveaway, 0);
        var components = GiveawayEmbedBuilder.BuildEnterButton().Build();

        // Tag role trong CONTENT (mention trong embed không ping được).
        // AllowedMentions chỉ định đúng role này — tránh ping lố.
        string? content = null;
        AllowedMentions? mentions = null;
        if (pingRoleId is not null)
        {
            content = $"<@&{pingRoleId.Value}>";
            mentions = new AllowedMentions { RoleIds = [pingRoleId.Value] };
        }

        var message = await channel.SendMessageAsync(
            text: content, embed: embed, components: components, allowedMentions: mentions);

        giveaway.MessageId = message.Id;
        await _repo.AddAsync(giveaway);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Created giveaway {Id} in guild {GuildId}, ends {EndsAt}",
            giveaway.Id, guildId, giveaway.EndsAt);

        return giveaway;
    }

    public async Task<(bool success, string message)> EnterAsync(ulong messageId, ulong userId, ulong guildId)
    {
        var giveaway = await _repo.GetByMessageIdAsync(messageId);
        if (giveaway is null || giveaway.IsEnded || giveaway.IsCancelled)
            return (false, "This giveaway is no longer active.");

        if (giveaway.HostId == userId)
            return (false, "You cannot enter your own giveaway.");

        if (await _repo.HasEnteredAsync(giveaway.Id, userId))
            return (false, "You have already entered this giveaway!");

        // Điều kiện tham gia — cần ngày join nếu có bất kỳ requirement nào
        if (giveaway.MinJoinDays > 0 || giveaway.JoinedBefore is not null)
        {
            var member = _client.GetGuild(guildId)?.GetUser(userId);
            var joinedAt = member?.JoinedAt;

            // Cache miss — thử REST trước khi kết luận
            if (joinedAt is null)
            {
                try
                {
                    var restMember = await _client.Rest.GetGuildUserAsync(guildId, userId);
                    joinedAt = restMember?.JoinedAt;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch JoinedAt for user {UserId}", userId);
                }
            }

            if (joinedAt is null)
                return (false, "Could not verify your join date. Please try again later.");

            if (giveaway.MinJoinDays > 0)
            {
                var daysInServer = (DateTimeOffset.UtcNow - joinedAt.Value).TotalDays;
                if (daysInServer < giveaway.MinJoinDays)
                    return (false,
                        $"❌ You don't meet the requirement: you must be in this server for " +
                        $"**{giveaway.MinJoinDays} days** to enter. You've been here **{(int)daysInServer} day(s)**.");
            }

            // Mốc ngày cố định: phải join TRƯỚC ngày này
            if (giveaway.JoinedBefore is not null
                && joinedAt.Value.UtcDateTime >= giveaway.JoinedBefore.Value)
                return (false,
                    $"❌ This giveaway requires you to have joined the server before " +
                    $"**{giveaway.JoinedBefore.Value:dd/MM/yyyy}**. You joined on **{joinedAt.Value:dd/MM/yyyy}**.");
        }

        var entry = new GiveawayEntry
        {
            GiveawayId = giveaway.Id,
            GuildId = guildId,
            UserId = userId,
            EnteredAt = DateTime.UtcNow
        };

        await _repo.AddEntryAsync(entry);
        await _repo.SaveChangesAsync();

        var entryCount = await _repo.GetEntryCountAsync(giveaway.Id);
        await UpdateEmbedAsync(giveaway, entryCount);

        return (true, "You have entered the giveaway! 🎉");
    }

    // Server Owner pre-selects winners — giveaway vẫn chạy bình thường đến hết giờ
    // Gọi nhiều lần để add từng người, max = WinnerCount
    public async Task<(bool success, string message)> PreSelectWinnerAsync(
        Guid giveawayId, ulong winnerId, ulong requesterId, ulong guildId)
    {
        var giveaway = await _repo.GetByIdAsync(giveawayId);
        if (giveaway is null) return (false, "Giveaway not found.");
        if (giveaway.IsEnded) return (false, "Giveaway has already ended.");
        if (giveaway.IsCancelled) return (false, "Giveaway has been cancelled.");

        var guild = _client.GetGuild(guildId);
        var member = guild?.GetUser(winnerId);
        if (member is null) return (false, "User not found in this server.");

        var current = giveaway.GetPreSelectedWinners();

        if (current.Contains(winnerId))
            return (false, $"<@{winnerId}> is already in the pre-selected list.");

        if (current.Count >= giveaway.WinnerCount)
            return (false, $"Already have {giveaway.WinnerCount} pre-selected winner(s) — matches WinnerCount. Use `/giveaway unpick` to remove someone first.");

        current.Add(winnerId);
        giveaway.PreSelectedWinnerIds = string.Join(",", current);
        giveaway.PreSelectedAt = DateTime.UtcNow;
        giveaway.PreSelectedBy = requesterId;
        await _repo.SaveChangesAsync();

        _logger.LogInformation(
            "Giveaway {Id} pre-selected winners updated: [{Winners}] by {RequesterId}",
            giveawayId, giveaway.PreSelectedWinnerIds, requesterId);

        var allMentions = string.Join(", ", current.Select(id => $"<@{id}>"));
        await SendGiveawayAuditAsync(guildId,
            $"🎯 **[AUDIT] Giveaway Winner Pre-Selected**\n" +
            $"**Giveaway:** {giveaway.Prize} (`{giveaway.Id}`)\n" +
            $"**Added:** <@{winnerId}>\n" +
            $"**All pre-selected ({current.Count}/{giveaway.WinnerCount}):** {allMentions}\n" +
            $"**Selected by:** <@{requesterId}>\n" +
            $"**Giveaway ends:** <t:{new DateTimeOffset(giveaway.EndsAt).ToUnixTimeSeconds()}:R>\n" +
            $"*Winners will be announced when giveaway ends — participants are unaware.*");

        return (true, $"Added <@{winnerId}> to pre-selected list. ({current.Count}/{giveaway.WinnerCount} slots filled)");
    }

    // Clear pre-selection — truyền winnerId để remove 1 người, null để clear all
    public async Task<(bool success, string message)> ClearPreSelectionAsync(
        Guid giveawayId, ulong requesterId, ulong guildId, ulong? winnerId = null)
    {
        var giveaway = await _repo.GetByIdAsync(giveawayId);
        if (giveaway is null) return (false, "Giveaway not found.");
        if (giveaway.IsEnded) return (false, "Giveaway has already ended.");

        var current = giveaway.GetPreSelectedWinners();
        if (current.Count == 0) return (false, "No pre-selection to clear.");

        string auditDetail;

        if (winnerId.HasValue)
        {
            if (!current.Remove(winnerId.Value))
                return (false, $"<@{winnerId.Value}> is not in the pre-selected list.");

            giveaway.PreSelectedWinnerIds = current.Count > 0
                ? string.Join(",", current)
                : null;
            if (current.Count == 0) { giveaway.PreSelectedAt = null; giveaway.PreSelectedBy = null; }

            auditDetail = $"**Removed:** <@{winnerId.Value}>\n" +
                          $"**Remaining ({current.Count}/{giveaway.WinnerCount}):** " +
                          (current.Count > 0 ? string.Join(", ", current.Select(id => $"<@{id}>")) : "none");
        }
        else
        {
            var prev = string.Join(", ", current.Select(id => $"<@{id}>"));
            giveaway.PreSelectedWinnerIds = null;
            giveaway.PreSelectedAt = null;
            giveaway.PreSelectedBy = null;
            auditDetail = $"**Cleared all:** {prev}";
        }

        await _repo.SaveChangesAsync();

        await SendGiveawayAuditAsync(guildId,
            $"🔄 **[AUDIT] Giveaway Pre-Selection Updated**\n" +
            $"**Giveaway:** {giveaway.Prize} (`{giveaway.Id}`)\n" +
            auditDetail + "\n" +
            $"**By:** <@{requesterId}>");

        return winnerId.HasValue
            ? (true, $"Removed <@{winnerId.Value}> from pre-selected list.")
            : (true, "All pre-selections cleared. Giveaway will pick random winners.");
    }

    public async Task EndGiveawayAsync(Giveaway giveaway)
    {
        if (giveaway.IsEnded) return;

        List<ulong> winners;
        var preSelected = giveaway.GetPreSelectedWinners();
        bool wasPreSelected = preSelected.Count > 0;

        if (wasPreSelected)
        {
            // Pre-selected winners — bỏ qua random
            winners = preSelected;
            _logger.LogInformation("Giveaway {Id} ended with pre-selected winners [{Winners}]",
                giveaway.Id, giveaway.PreSelectedWinnerIds);
        }
        else
        {
            // Random như bình thường
            var entries = await _repo.GetEntriesAsync(giveaway.Id);
            winners = PickWinners(entries, giveaway.WinnerCount);
        }

        giveaway.IsEnded = true;
        giveaway.WinnerIds = string.Join(",", winners.Select(w => w.ToString()));
        await _repo.SaveChangesAsync();

        var guild = _client.GetGuild(giveaway.GuildId);
        var channel = guild?.GetTextChannel(giveaway.ChannelId);
        if (channel is null) return;

        var winnerMentions = winners.Select(id => $"<@{id}>").ToList();

        try
        {
            var msg = await channel.GetMessageAsync(giveaway.MessageId) as IUserMessage;
            if (msg is not null)
            {
                var endedEmbed = GiveawayEmbedBuilder.BuildEndedEmbed(giveaway, winnerMentions);
                var disabledBtn = GiveawayEmbedBuilder.BuildEnterButton(disabled: true).Build();
                await msg.ModifyAsync(p =>
                {
                    p.Embed = endedEmbed;
                    p.Components = disabledBtn;
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not edit giveaway message {MessageId}", giveaway.MessageId);
        }

        if (winners.Count > 0)
        {
            // Công bố lần lượt từng winner, cách nhau 2 giây
            var medals = new[] { "🥇", "🥈", "🥉", "🏅", "🏅", "🏅", "🏅", "🏅", "🏅", "🏅" };
            if (winners.Count == 1)
            {
                await channel.SendMessageAsync(
                    $"🎉 Congratulations <@{winners[0]}>! You won **{giveaway.Prize}**!");
            }
            else
            {
                await channel.SendMessageAsync($"🎊 **{giveaway.Prize}** — Announcing winners...");
                for (int i = 0; i < winners.Count; i++)
                {
                    await Task.Delay(2000);
                    var medal = i < medals.Length ? medals[i] : "🏅";
                    await channel.SendMessageAsync(
                        $"{medal} **Winner #{i + 1}:** <@{winners[i]}> — Congratulations!");
                }
            }

            // DM riêng từng người trúng — hướng dẫn nhận thưởng
            var failedDms = await DmWinnersAsync(guild!, giveaway, winners);
            if (failedDms.Count > 0)
            {
                await channel.SendMessageAsync(
                    $"⚠️ Could not DM {string.Join(", ", failedDms.Select(id => $"<@{id}>"))} " +
                    "— please open your DMs and contact the staff to claim your reward.",
                    allowedMentions: AllowedMentions.None);
            }
        }
        else
        {
            await channel.SendMessageAsync($"😔 No one entered the giveaway for **{giveaway.Prize}**.");
        }

        // Audit log khi end — ghi rõ nếu winner được pre-selected
        var entryCount = await _repo.GetEntryCountAsync(giveaway.Id);
        if (wasPreSelected)
        {
            await SendGiveawayAuditAsync(giveaway.GuildId,
                $"✅ **[AUDIT] Giveaway Ended — Pre-Selected Winner**\n" +
                $"**Giveaway:** {giveaway.Prize} (`{giveaway.Id}`)\n" +
                $"**Winner:** {string.Join(", ", winnerMentions)}\n" +
                $"**Pre-selected by:** <@{giveaway.PreSelectedBy}> at <t:{new DateTimeOffset(giveaway.PreSelectedAt!.Value).ToUnixTimeSeconds()}:F>\n" +
                $"**Total entries:** {entryCount}\n" +
                $"⚠️ *Winner was manually pre-selected, not randomly drawn.*");
        }
        else
        {
            await SendGiveawayAuditAsync(giveaway.GuildId,
                $"✅ **[AUDIT] Giveaway Ended — Random Draw**\n" +
                $"**Giveaway:** {giveaway.Prize} (`{giveaway.Id}`)\n" +
                $"**Winner(s):** {(winners.Count > 0 ? string.Join(", ", winnerMentions) : "none")}\n" +
                $"**Total entries:** {entryCount}");
        }
    }

    public async Task<(bool success, string message)> RerollAsync(Guid giveawayId, ulong requesterId)
    {
        var giveaway = await _repo.GetByIdAsync(giveawayId);
        if (giveaway is null) return (false, "Giveaway not found.");
        if (!giveaway.IsEnded) return (false, "Giveaway has not ended yet.");

        var entries = await _repo.GetEntriesAsync(giveaway.Id);
        if (entries.Count == 0) return (false, "No entries to reroll from.");

        var prevWinners = giveaway.WinnerIds;
        var newWinners = PickWinners(entries, giveaway.WinnerCount);
        giveaway.WinnerIds = string.Join(",", newWinners.Select(w => w.ToString()));
        await _repo.SaveChangesAsync();

        var guild = _client.GetGuild(giveaway.GuildId);
        var channel = guild?.GetTextChannel(giveaway.ChannelId);
        if (channel is not null)
        {
            var mentions = newWinners.Select(id => $"<@{id}>").ToList();
            await channel.SendMessageAsync(
                $"🔄 **Reroll!** New winner(s) for **{giveaway.Prize}**: {string.Join(", ", mentions)}");
        }

        // Audit log reroll
        await SendGiveawayAuditAsync(giveaway.GuildId,
            $"🔄 **[AUDIT] Giveaway Rerolled**\n" +
            $"**Giveaway:** {giveaway.Prize} (`{giveaway.Id}`)\n" +
            $"**Previous winners:** {prevWinners ?? "none"}\n" +
            $"**New winners:** {giveaway.WinnerIds}\n" +
            $"**Rerolled by:** <@{requesterId}>");

        return (true, "Rerolled successfully.");
    }

    public async Task<bool> CancelAsync(Guid giveawayId, ulong requesterId = 0)
    {
        var giveaway = await _repo.GetByIdAsync(giveawayId);
        if (giveaway is null || giveaway.IsEnded) return false;

        giveaway.IsCancelled = true;
        await _repo.SaveChangesAsync();

        var guild = _client.GetGuild(giveaway.GuildId);
        var channel = guild?.GetTextChannel(giveaway.ChannelId);
        if (channel is not null)
        {
            try
            {
                var msg = await channel.GetMessageAsync(giveaway.MessageId) as IUserMessage;
                if (msg is not null)
                {
                    var cancelEmbed = GiveawayEmbedBuilder.BuildCancelledEmbed(giveaway);
                    var disabledBtn = GiveawayEmbedBuilder.BuildEnterButton(disabled: true).Build();
                    await msg.ModifyAsync(p =>
                    {
                        p.Embed = cancelEmbed;
                        p.Components = disabledBtn;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not edit cancelled giveaway message");
            }
        }

        // Audit log cancel
        if (requesterId != 0)
        {
            await SendGiveawayAuditAsync(giveaway.GuildId,
                $"❌ **[AUDIT] Giveaway Cancelled**\n" +
                $"**Giveaway:** {giveaway.Prize} (`{giveaway.Id}`)\n" +
                $"**Cancelled by:** <@{requesterId}>");
        }

        return true;
    }

    // DM từng winner: chúc mừng + hướng dẫn nhận thưởng (ClaimMessage tùy chỉnh).
    // Trả về danh sách userId DM thất bại (đóng DM / chặn bot) để announce nhắc.
    private async Task<List<ulong>> DmWinnersAsync(
        SocketGuild guild, Giveaway giveaway, List<ulong> winners)
    {
        var failed = new List<ulong>();

        var claimText = giveaway.ClaimMessage
            ?? "Please contact the server staff to claim your reward.";

        var embed = new EmbedBuilder()
            .WithTitle("🎉 You won a giveaway!")
            .WithDescription(
                $"Congratulations! You won **{giveaway.Prize}** in **{guild.Name}**!\n\n" +
                $"**How to claim your reward:**\n{claimText}")
            .WithColor(new Color(0xF5A623))
            .WithFooter(guild.Name)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        foreach (var winnerId in winners)
        {
            try
            {
                var user = guild.GetUser(winnerId)
                    ?? (IUser?)await _client.Rest.GetUserAsync(winnerId);
                if (user is null) { failed.Add(winnerId); continue; }

                var dm = await user.CreateDMChannelAsync();
                await dm.SendMessageAsync(embed: embed);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex,
                    "Could not DM giveaway winner {UserId} for giveaway {Id}", winnerId, giveaway.Id);
                failed.Add(winnerId);
            }
        }

        return failed;
    }

    private async Task UpdateEmbedAsync(Giveaway giveaway, int entryCount)
    {
        try
        {
            var guild = _client.GetGuild(giveaway.GuildId);
            var channel = guild?.GetTextChannel(giveaway.ChannelId);
            if (channel is null) return;

            var msg = await channel.GetMessageAsync(giveaway.MessageId) as IUserMessage;
            if (msg is null) return;

            var embed = GiveawayEmbedBuilder.BuildActiveEmbed(giveaway, entryCount);
            await msg.ModifyAsync(p => p.Embed = embed);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not update giveaway embed for {MessageId}", giveaway.MessageId);
        }
    }

    private async Task SendGiveawayAuditAsync(ulong guildId, string message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();
            var auditChannelId = await logService.GetLogChannelAsync(guildId, LogCategory.Audit);
            if (auditChannelId is null) return;

            var guild = _client.GetGuild(guildId);
            var channel = guild?.GetTextChannel(auditChannelId.Value);
            if (channel is null) return;

            await channel.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send giveaway audit log for guild {GuildId}", guildId);
        }
    }

    public async Task<(bool success, string message)> EndEarlyAsync(Guid giveawayId, ulong requesterId, ulong guildId)
    {
        var giveaway = await _repo.GetByIdAsync(giveawayId);
        if (giveaway is null || giveaway.GuildId != guildId) return (false, "Giveaway not found.");
        if (giveaway.IsEnded) return (false, "Giveaway has already ended.");
        if (giveaway.IsCancelled) return (false, "Giveaway has been cancelled.");

        _logger.LogInformation("Giveaway {Id} ended early by {RequesterId}", giveawayId, requesterId);
        await EndGiveawayAsync(giveaway);
        return (true, "Giveaway ended early — winners announced!");
    }

    public Task<List<Giveaway>> ListActiveAsync(ulong guildId)
        => _repo.GetActiveByGuildAsync(guildId);

    public Task<Giveaway?> GetByIdAsync(Guid id)
        => _repo.GetByIdAsync(id);

    public Task<List<GiveawayEntry>> GetEntriesAsync(Guid giveawayId)
        => _repo.GetEntriesAsync(giveawayId);

    private static List<ulong> PickWinners(List<GiveawayEntry> entries, int count)
    {
        if (entries.Count == 0) return [];
        var shuffled = entries.OrderBy(_ => _rng.Next()).ToList();
        return shuffled.Take(Math.Min(count, shuffled.Count))
                       .Select(e => e.UserId)
                       .ToList();
    }
}