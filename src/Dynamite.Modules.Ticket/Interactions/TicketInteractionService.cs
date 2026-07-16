// src/Dynamite.Modules.Ticket/Interactions/TicketInteractionService.cs
namespace Dynamite.Modules.Ticket.Interactions;

using Discord;
using Discord.WebSocket;
using Dynamite.Modules.Ticket.Helpers;
using Dynamite.Modules.Ticket.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class TicketInteractionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketInteractionService> _logger;

    public TicketInteractionService(
        IServiceScopeFactory scopeFactory,
        ILogger<TicketInteractionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleButtonAsync(SocketMessageComponent interaction)
    {
        var customId = interaction.Data.CustomId;

        if (customId == TicketEmbedBuilder.OpenButtonId)
            await HandleOpenAsync(interaction);
        else if (customId == TicketEmbedBuilder.CloseButtonId)
            await HandleCloseAsync(interaction);
        else if (customId == TicketEmbedBuilder.DeleteButtonId)
            await HandleDeleteAsync(interaction);
    }

    private async Task HandleOpenAsync(SocketMessageComponent interaction)
    {
        await interaction.DeferAsync(ephemeral: true);

        var guildUser = interaction.User as IGuildUser;
        if (guildUser is null && interaction.Channel is IGuildChannel guildChannel)
        {
            guildUser = await guildChannel.Guild.GetUserAsync(interaction.User.Id);
        }

        if (guildUser is null)
        {
            await interaction.FollowupAsync("Could not resolve your member profile on this server. Please try again.", ephemeral: true);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TicketService>();

        try
        {
            var result = await service.OpenTicketAsync(guildUser.Guild.Id, interaction.User.Id);
            await interaction.FollowupAsync(
                result ? $"Your ticket has been created: <#{result.Value}>" : result.ErrorMessage,
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open ticket for user {UserId}", interaction.User.Id);
            await interaction.FollowupAsync("An error occurred while opening the ticket.", ephemeral: true);
        }
    }

    private async Task HandleCloseAsync(SocketMessageComponent interaction)
    {
        await interaction.DeferAsync(ephemeral: true);

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TicketService>();

        try
        {
            var result = await service.CloseTicketAsync(
                interaction.Channel.Id,
                interaction.User.Id);

            if (!result)
                await interaction.FollowupAsync(result.ErrorMessage, ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close ticket in channel {ChannelId}", interaction.Channel.Id);
            await interaction.FollowupAsync("An error occurred while closing the ticket.", ephemeral: true);
        }
    }

    private async Task HandleDeleteAsync(SocketMessageComponent interaction)
    {
        await interaction.DeferAsync(ephemeral: true);

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TicketService>();

        try
        {
            await service.DeleteTicketAsync(
                interaction.Channel.Id,
                interaction.User.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete ticket in channel {ChannelId}", interaction.Channel.Id);
            await interaction.FollowupAsync("An error occurred while deleting the ticket.", ephemeral: true);
        }
    }
}