// src/Dynamite.Modules.Giveaway/Interactions/GiveawayInteractionService.cs
namespace Dynamite.Modules.Giveaway.Interactions;

using Discord;
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
        var service = scope.ServiceProvider.GetRequiredService<GiveawayService>();

        var messageId = interaction.Message.Id;
        var userId = interaction.User.Id;
        var guildId = guildUser.Guild.Id;

        try
        {
            var result = await service.EnterAsync(messageId, userId, guildId);
            await interaction.FollowupAsync(
                result ? "You have entered the giveaway! 🎉" : result.ErrorMessage,
                ephemeral: true);

            _logger.LogDebug("User {UserId} attempted giveaway entry: {Result}", userId, result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enter giveaway for user {UserId}", userId);
            await interaction.FollowupAsync("An error occurred while entering the giveaway.", ephemeral: true);
        }
    }
}