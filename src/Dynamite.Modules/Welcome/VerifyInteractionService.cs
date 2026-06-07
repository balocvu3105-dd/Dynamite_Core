// src/Dynamite.Modules/Welcome/VerifyInteractionService.cs
namespace Dynamite.Modules.Welcome;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Welcome.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class VerifyInteractionService
{
    public const string VerifyButtonId = "verify:confirm";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VerifyInteractionService> _logger;

    public VerifyInteractionService(
        IServiceScopeFactory scopeFactory,
        ILogger<VerifyInteractionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleVerifyAsync(SocketMessageComponent interaction)
    {
        if (interaction.User is not SocketGuildUser guildUser) return;

        await interaction.DeferAsync(ephemeral: true);

        using var scope = _scopeFactory.CreateScope();
        var welcomeService = scope.ServiceProvider.GetRequiredService<IWelcomeService>();

        var (roleId, configured) = await welcomeService.GetVerifyRoleAsync(guildUser.Guild.Id);

        if (!configured || roleId is null)
        {
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Error(
                    "Not Configured",
                    "Verification role has not been set up. Please contact an admin."),
                ephemeral: true);
            return;
        }

        // Cek apakah sudah punya role
        if (guildUser.Roles.Any(r => r.Id == roleId.Value))
        {
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Info(
                    "Already Verified",
                    "You are already verified!"),
                ephemeral: true);
            return;
        }

        try
        {
            await guildUser.AddRoleAsync(roleId.Value);

            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Success(
                    "✅ Verified!",
                    $"Welcome to **{guildUser.Guild.Name}**! You now have access to the server."),
                ephemeral: true);

            _logger.LogInformation("User {UserId} verified in guild {GuildId}",
                guildUser.Id, guildUser.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign verify role to user {UserId}", guildUser.Id);
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Error(
                    "Error",
                    "Failed to assign the verified role. Please contact an admin."),
                ephemeral: true);
        }
    }
}