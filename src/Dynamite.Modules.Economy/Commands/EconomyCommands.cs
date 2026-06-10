// src/Dynamite.Modules.Economy/Commands/EconomyCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;

public class EconomyCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly WalletService _wallet;

    public EconomyCommands(WalletService wallet)
    {
        _wallet = wallet;
    }

    [SlashCommand("daily", "Claim your daily coin reward")]
    public async Task DailyAsync()
    {
        await DeferAsync(ephemeral: true);

        var (success, message, coins, streak) = await _wallet.ClaimDailyAsync(
            Context.Guild.Id, Context.User.Id);

        if (!success)
        {
            await FollowupAsync(message, ephemeral: true);
            return;
        }

        var wallet = await _wallet.GetWalletAsync(Context.Guild.Id, Context.User.Id);
        var embed = EconomyEmbedBuilder.BuildDailyEmbed(coins, wallet.Coins, streak);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("balance", "Check your or another user's balance")]
    public async Task BalanceAsync(
        [Summary("user", "User to check (default: yourself)")] IGuildUser? user = null)
    {
        await DeferAsync(ephemeral: true);

        var target = user ?? (IGuildUser)Context.User;
        var wallet = await _wallet.GetWalletAsync(Context.Guild.Id, target.Id);
        var embed = EconomyEmbedBuilder.BuildBalanceEmbed(wallet, target.DisplayName);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("transfer", "Transfer coins to another user")]
    public async Task TransferAsync(
        [Summary("user", "User to send coins to")] IGuildUser user,
        [Summary("amount", "Amount to transfer")] long amount)
    {
        await DeferAsync(ephemeral: true);

        var (success, message) = await _wallet.TransferAsync(
            Context.Guild.Id, Context.User.Id, user.Id, amount);

        await FollowupAsync(success ? message : $"❌ {message}", ephemeral: true);
    }

    [SlashCommand("leaderboard", "View the top coin holders")]
    public async Task LeaderboardAsync()
    {
        await DeferAsync();

        var entries = await _wallet.GetLeaderboardAsync(Context.Guild.Id);
        var embed = EconomyEmbedBuilder.BuildLeaderboardEmbed(entries, Context.Guild.Id);
        await FollowupAsync(embed: embed);
    }
}