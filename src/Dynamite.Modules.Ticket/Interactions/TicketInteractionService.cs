// src/Dynamite.Modules.Ticket/Interactions/TicketInteractionService.cs
namespace Dynamite.Modules.Ticket.Interactions;

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

        var guild = ((SocketGuildChannel)interaction.Channel).Guild;
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TicketService>();

        var (success, message) = await service.OpenTicketAsync(guild.Id, interaction.User.Id);
        await interaction.FollowupAsync(message, ephemeral: true);
    }

    private async Task HandleCloseAsync(SocketMessageComponent interaction)
    {
        await interaction.DeferAsync(ephemeral: true);

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TicketService>();

        var (success, message) = await service.CloseTicketAsync(
            interaction.Channel.Id,
            interaction.User.Id);

        if (!success)
            await interaction.FollowupAsync(message, ephemeral: true);
        // Nếu success thì channel đã được rename, không cần followup thêm
    }

    private async Task HandleDeleteAsync(SocketMessageComponent interaction)
    {
        // Không defer vì channel sẽ bị xóa ngay — chỉ staff mới thấy nút này
        await interaction.DeferAsync(ephemeral: true);

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TicketService>();

        await service.DeleteTicketAsync(
            interaction.Channel.Id,
            interaction.User.Id);

        // Channel đã bị xóa nên không cần followup
    }
}