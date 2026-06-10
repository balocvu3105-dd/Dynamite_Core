// src/Dynamite.Modules.Giveaway/Interactions/GiveawayInteractionService.cs
namespace Dynamite.Modules.Giveaway.Interactions;

using Discord.WebSocket;
using Dynamite.Modules.Giveaway.Helpers;
using Dynamite.Modules.Giveaway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class GiveawayInteractionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GiveawayInteractionService> _logger;

    public GiveawayInteractionService(
        IServiceScopeFactory scopeFactory,
        ILogger<GiveawayInteractionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleButtonAsync(SocketMessageComponent interaction)
    {
        if (interaction.Data.CustomId != GiveawayEmbedBuilder.EnterButtonId) return;

        await interaction.DeferAsync(ephemeral: true);

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<GiveawayService>();

        var messageId = interaction.Message.Id;
        var userId = interaction.User.Id;
        var guildId = ((SocketGuildChannel)interaction.Channel).Guild.Id;

        var (success, message) = await service.EnterAsync(messageId, userId, guildId);

        await interaction.FollowupAsync(message, ephemeral: true);

        _logger.LogDebug("User {UserId} attempted giveaway entry: {Result}", userId, success);
    }
}