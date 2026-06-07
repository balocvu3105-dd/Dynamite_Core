// src/Dynamite.Modules/Welcome/Modules/VerifyConfigModule.cs
namespace Dynamite.Modules.Welcome.Modules;

using Discord;
using Discord.Interactions;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Welcome.Helpers;
using Microsoft.Extensions.Logging;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
[Group("verify", "Configure the verification system")]
public class VerifyConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IWelcomeService _welcomeService;
    private readonly ILogger<VerifyConfigModule> _logger;

    public VerifyConfigModule(IWelcomeService welcomeService, ILogger<VerifyConfigModule> logger)
    {
        _welcomeService = welcomeService;
        _logger = logger;
    }

    [SlashCommand("setup", "Post the verification panel in a channel")]
    public async Task SetupAsync(
        [Summary("channel", "Channel to post the verify panel")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        // Check verify role đã set chưa
        var (roleId, configured) = await _welcomeService.GetVerifyRoleAsync(Context.Guild.Id);
        if (!configured)
        {
            await FollowupAsync(embed: WelcomeEmbeds.Error(
                "Role Not Set",
                "Please set a verify role first with `/verify set-role`."), ephemeral: true);
            return;
        }

        // Build verify panel
        var embed = WelcomeEmbeds.VerifyPanel(Context.Guild.Name);
        var component = new ComponentBuilder()
            .WithButton(
                label: "✅ Verify",
                customId: VerifyInteractionService.VerifyButtonId,
                style: ButtonStyle.Success)
            .Build();

        await channel.SendMessageAsync(embed: embed, components: component);

        // Save channel to config
        await _welcomeService.SetVerifyChannelAsync(
            Context.Guild.Id, Context.Guild.Name, channel.Id);

        await FollowupAsync(embed: WelcomeEmbeds.Success(
            "Verify Panel Posted",
            $"Verification panel has been posted in {channel.Mention}."), ephemeral: true);

        _logger.LogInformation("Verify panel posted in channel {ChannelId} for guild {GuildId}",
            channel.Id, Context.Guild.Id);
    }

    [SlashCommand("set-role", "Set the role given to verified members")]
    public async Task SetRoleAsync(
        [Summary("role", "The verified role")] IRole role)
    {
        await DeferAsync(ephemeral: true);

        // Validate bot có thể assign role này không
        var botUser = Context.Guild.CurrentUser;
        if (botUser.Hierarchy <= role.Position)
        {
            await FollowupAsync(embed: WelcomeEmbeds.Error(
                "Role Too High",
                $"My highest role must be above {role.Mention} to assign it."), ephemeral: true);
            return;
        }

        if (role.Id == Context.Guild.EveryoneRole.Id)
        {
            await FollowupAsync(embed: WelcomeEmbeds.Error(
                "Invalid Role",
                "Cannot use @everyone as the verify role."), ephemeral: true);
            return;
        }

        await _welcomeService.SetVerifyRoleAsync(
            Context.Guild.Id, Context.Guild.Name, role.Id);

        await FollowupAsync(embed: WelcomeEmbeds.Success(
            "Verify Role Set",
            $"{role.Mention} will be given to members after they verify."), ephemeral: true);
    }
}