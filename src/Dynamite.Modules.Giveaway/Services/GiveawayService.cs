// src/Dynamite.Modules.Giveaway/Services/GiveawayService.cs
namespace Dynamite.Modules.Giveaway.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Giveaway.Helpers;
using Microsoft.Extensions.Logging;

public class GiveawayService
{
    private readonly IGiveawayRepository _repo;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<GiveawayService> _logger;
    private static readonly Random _rng = new();

    public GiveawayService(
        IGiveawayRepository repo,
        DiscordSocketClient client,
        ILogger<GiveawayService> logger)
    {
        _repo = repo;
        _client = client;
        _logger = logger;
    }

    public async Task<Giveaway> CreateAsync(
        ulong guildId,
        ulong channelId,
        ulong hostId,
        string prize,
        string? description,
        int winnerCount,
        TimeSpan duration)
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
            MessageId = 0 // placeholder, updated after send
        };

        // Send embed first to get messageId
        var embed = GiveawayEmbedBuilder.BuildActiveEmbed(giveaway, 0);
        var components = GiveawayEmbedBuilder.BuildEnterButton().Build();

        var message = await channel.SendMessageAsync(embed: embed, components: components);

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

        var entry = new GiveawayEntry
        {
            GiveawayId = giveaway.Id,
            GuildId = guildId,
            UserId = userId,
            EnteredAt = DateTime.UtcNow
        };

        await _repo.AddEntryAsync(entry);
        await _repo.SaveChangesAsync();

        // Update embed entry count
        var entryCount = await _repo.GetEntryCountAsync(giveaway.Id);
        await UpdateEmbedAsync(giveaway, entryCount);

        return (true, "You have entered the giveaway! 🎉");
    }

    public async Task EndGiveawayAsync(Giveaway giveaway)
    {
        if (giveaway.IsEnded) return;

        var entries = await _repo.GetEntriesAsync(giveaway.Id);
        var winners = PickWinners(entries, giveaway.WinnerCount);

        giveaway.IsEnded = true;
        giveaway.WinnerIds = string.Join(",", winners.Select(w => w.ToString()));

        await _repo.SaveChangesAsync();

        var guild = _client.GetGuild(giveaway.GuildId);
        var channel = guild?.GetTextChannel(giveaway.ChannelId);
        if (channel is null) return;

        var winnerMentions = winners.Select(id => $"<@{id}>").ToList();

        // Edit original message
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

        // Announce winners
        if (winners.Count > 0)
        {
            var announce = $"🎉 Congratulations {string.Join(", ", winnerMentions)}! " +
                           $"You won **{giveaway.Prize}**!";
            await channel.SendMessageAsync(announce);
        }
        else
        {
            await channel.SendMessageAsync($"😔 No one entered the giveaway for **{giveaway.Prize}**.");
        }

        _logger.LogInformation("Ended giveaway {Id}, winners: {Winners}",
            giveaway.Id, giveaway.WinnerIds ?? "none");
    }

    public async Task<(bool success, string message)> RerollAsync(Guid giveawayId, ulong requesterId)
    {
        var giveaway = await _repo.GetByIdAsync(giveawayId);
        if (giveaway is null) return (false, "Giveaway not found.");
        if (!giveaway.IsEnded) return (false, "Giveaway has not ended yet.");

        var entries = await _repo.GetEntriesAsync(giveaway.Id);
        if (entries.Count == 0) return (false, "No entries to reroll from.");

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

        return (true, "Rerolled successfully.");
    }

    public async Task<bool> CancelAsync(Guid giveawayId)
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

        return true;
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

    private static List<ulong> PickWinners(List<GiveawayEntry> entries, int count)
    {
        if (entries.Count == 0) return [];

        var shuffled = entries.OrderBy(_ => _rng.Next()).ToList();
        return shuffled.Take(Math.Min(count, shuffled.Count))
                       .Select(e => e.UserId)
                       .ToList();
    }
}