// src/Dynamite.Modules.Economy/Commands/FishingCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord.Interactions;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;

public class FishingCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly FishingService _fishing;

    public FishingCommands(FishingService fishing)
    {
        _fishing = fishing;
    }

    [SlashCommand("fish", "Go fishing and earn coins!")]
    public async Task FishAsync()
    {
        await DeferAsync();

        var (success, cooldownMsg, result, totalCoins, rodName) =
            await _fishing.FishAsync(Context.Guild.Id, Context.User.Id);

        if (!success)
        {
            await FollowupAsync(cooldownMsg, ephemeral: true);
            return;
        }

        var embed = EconomyEmbedBuilder.BuildFishEmbed(result!, totalCoins, rodName);
        await FollowupAsync(embed: embed);
    }
}