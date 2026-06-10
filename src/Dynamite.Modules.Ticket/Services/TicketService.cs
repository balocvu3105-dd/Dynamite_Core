// src/Dynamite.Modules.Ticket/Services/TicketService.cs
namespace Dynamite.Modules.Ticket.Services;

using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Ticket.Helpers;
using Microsoft.Extensions.Logging;

public class TicketService
{
    private readonly ITicketRepository _repo;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository repo,
        DiscordSocketClient client,
        ILogger<TicketService> logger)
    {
        _repo = repo;
        _client = client;
        _logger = logger;
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    public async Task<(bool success, string message)> SetupAsync(
        ulong guildId,
        ulong panelChannelId,
        ulong? staffRoleId,
        ulong? categoryId,
        string? description)
    {
        var guild = _client.GetGuild(guildId)
            ?? throw new InvalidOperationException("Guild not found.");

        var panelChannel = guild.GetTextChannel(panelChannelId)
            ?? throw new InvalidOperationException("Panel channel not found.");

        // Nếu đã có config thì update panel message cũ
        var existing = await _repo.GetConfigAsync(guildId);
        if (existing is not null)
        {
            // Xóa message panel cũ nếu còn tồn tại
            try
            {
                var oldMsg = await panelChannel.GetMessageAsync(existing.PanelMessageId);
                if (oldMsg is not null) await oldMsg.DeleteAsync();
            }
            catch { /* ignore nếu message đã bị xóa */ }

            existing.PanelChannelId = panelChannelId;
            existing.StaffRoleId = staffRoleId;
            existing.CategoryId = categoryId;

            var newMsg = await panelChannel.SendMessageAsync(
                embed: TicketEmbedBuilder.BuildPanelEmbed(description),
                components: TicketEmbedBuilder.BuildPanelComponents());

            existing.PanelMessageId = newMsg.Id;
            await _repo.SaveChangesAsync();

            return (true, $"Ticket panel updated in {panelChannel.Mention}.");
        }

        // Tạo config mới
        var message = await panelChannel.SendMessageAsync(
            embed: TicketEmbedBuilder.BuildPanelEmbed(description),
            components: TicketEmbedBuilder.BuildPanelComponents());

        var config = new TicketConfig
        {
            GuildId = guildId,
            PanelChannelId = panelChannelId,
            PanelMessageId = message.Id,
            StaffRoleId = staffRoleId,
            CategoryId = categoryId,
            NextTicketNumber = 1
        };

        await _repo.AddConfigAsync(config);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Ticket system setup for guild {GuildId}", guildId);
        return (true, $"✅ Ticket panel created in {panelChannel.Mention}.");
    }

    // ── Open Ticket ───────────────────────────────────────────────────────────

    public async Task<(bool success, string message)> OpenTicketAsync(
        ulong guildId,
        ulong userId,
        string? topic = null)
    {
        var config = await _repo.GetConfigAsync(guildId);
        if (config is null)
            return (false, "Ticket system is not set up in this server.");

        // One ticket per user
        var existing = await _repo.GetOpenTicketByOwnerAsync(guildId, userId);
        if (existing is not null)
            return (false, $"You already have an open ticket: <#{existing.ChannelId}>.");

        var guild = _client.GetGuild(guildId)
            ?? throw new InvalidOperationException("Guild not found.");

        // Tạo channel với permissions
        var ticketNumber = config.NextTicketNumber++;
        var channelName = $"ticket-{ticketNumber:D4}";

        var channelProps = new TextChannelProperties();
        if (config.CategoryId.HasValue)
        {
            var category = guild.GetCategoryChannel(config.CategoryId.Value);
            if (category is not null)
                channelProps.CategoryId = category.Id;
        }

        var newChannel = await guild.CreateTextChannelAsync(channelName, props =>
        {
            if (config.CategoryId.HasValue)
                props.CategoryId = config.CategoryId.Value;
        });

        // Set permission overwrites
        // @everyone — deny View
        await newChannel.AddPermissionOverwriteAsync(
            guild.EveryoneRole,
            new OverwritePermissions(viewChannel: PermValue.Deny));

        // Ticket owner — allow View + Send + Read History
        var guildUser = guild.GetUser(userId);
        if (guildUser is not null)
        {
            await newChannel.AddPermissionOverwriteAsync(
                guildUser,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    readMessageHistory: PermValue.Allow));
        }

        // Staff role — allow View + Send + Read History + Manage Messages
        if (config.StaffRoleId.HasValue)
        {
            var staffRole = guild.GetRole(config.StaffRoleId.Value);
            if (staffRole is not null)
            {
                await newChannel.AddPermissionOverwriteAsync(
                    staffRole,
                    new OverwritePermissions(
                        viewChannel: PermValue.Allow,
                        sendMessages: PermValue.Allow,
                        readMessageHistory: PermValue.Allow,
                        manageMessages: PermValue.Allow));
            }
        }

        // Gửi embed trong channel mới
        await newChannel.SendMessageAsync(
            text: $"<@{userId}>",
            embed: TicketEmbedBuilder.BuildOpenedEmbed(userId, ticketNumber, topic),
            components: TicketEmbedBuilder.BuildOpenedComponents());

        // Lưu ticket vào DB
        var ticket = new Core.Entities.Ticket
        {
            TicketConfigId = config.Id,
            GuildId = guildId,
            ChannelId = newChannel.Id,
            OwnerId = userId,
            Number = ticketNumber,
            Topic = topic,
            Status = TicketStatus.Open,
            OpenedAt = DateTime.UtcNow
        };

        await _repo.AddTicketAsync(ticket);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Opened ticket #{Number} for user {UserId} in guild {GuildId}",
            ticketNumber, userId, guildId);

        return (true, $"Your ticket has been created: {newChannel.Mention}");
    }

    // ── Close Ticket ──────────────────────────────────────────────────────────

    public async Task<(bool success, string message)> CloseTicketAsync(
        ulong channelId,
        ulong closedById)
    {
        var ticket = await _repo.GetByChannelIdAsync(channelId);
        if (ticket is null || ticket.Status != TicketStatus.Open)
            return (false, "This is not an open ticket.");

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        var guild = _client.GetGuild(ticket.GuildId);
        var channel = guild?.GetTextChannel(channelId);
        if (channel is null) return (true, "Ticket closed.");

        // Rename channel
        await channel.ModifyAsync(p => p.Name = $"closed-{ticket.Number:D4}");

        // Remove owner's view permission
        var owner = guild!.GetUser(ticket.OwnerId);
        if (owner is not null)
        {
            await channel.AddPermissionOverwriteAsync(
                owner,
                new OverwritePermissions(viewChannel: PermValue.Deny));
        }

        // Gửi closed embed
        await channel.SendMessageAsync(
            embed: TicketEmbedBuilder.BuildClosedEmbed(closedById, ticket.Number),
            components: TicketEmbedBuilder.BuildClosedComponents());

        _logger.LogInformation("Closed ticket #{Number} in guild {GuildId}", ticket.Number, ticket.GuildId);
        return (true, "Ticket closed.");
    }

    // ── Delete Ticket ─────────────────────────────────────────────────────────

    public async Task<(bool success, string message)> DeleteTicketAsync(
        ulong channelId,
        ulong deletedById)
    {
        var ticket = await _repo.GetByChannelIdAsync(channelId);
        if (ticket is null || ticket.Status == TicketStatus.Deleted)
            return (false, "Ticket not found or already deleted.");

        ticket.Status = TicketStatus.Deleted;
        await _repo.SaveChangesAsync();

        var guild = _client.GetGuild(ticket.GuildId);
        var channel = guild?.GetTextChannel(channelId);

        if (channel is not null)
        {
            await channel.DeleteAsync(new RequestOptions
            {
                AuditLogReason = $"Ticket deleted by {deletedById}"
            });
        }

        _logger.LogInformation("Deleted ticket #{Number} in guild {GuildId}", ticket.Number, ticket.GuildId);
        return (true, "Ticket deleted.");
    }
}