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

        var result = await _wallet.ClaimDailyAsync(Context.Guild.Id, Context.User.Id);

        if (!result)
        {
            await FollowupAsync(result.ErrorMessage, ephemeral: true);
            return;
        }

        var d = result.Value!;
        // TotalCoins đã bao gồm coins vừa earn → không cần GetWalletAsync thêm
        var embed = EconomyEmbedBuilder.BuildDailyEmbed(d.CoinsEarned, d.TotalCoins, d.Streak);
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

        var result = await _wallet.TransferAsync(Context.Guild.Id, Context.User.Id, user.Id, amount);

        var response = result
            ? $"✅ Transferred **{amount:N0}** coins to {user.Mention}."
            : $"❌ {result.ErrorMessage}";
        await FollowupAsync(response, ephemeral: true);
    }

    [SlashCommand("richest", "Top người giàu nhất server (coins)")]
    public async Task LeaderboardAsync()
    {
        await DeferAsync();

        var excluded = new HashSet<ulong> { Context.Guild.OwnerId };
        foreach (var member in Context.Guild.Users)
            if (member.GuildPermissions.Administrator)
                excluded.Add(member.Id);

        var entries = (await _wallet.GetLeaderboardAsync(Context.Guild.Id))
            .Where(e => !excluded.Contains(e.userId))
            .ToList();

        var embed = EconomyEmbedBuilder.BuildLeaderboardEmbed(entries, Context.Guild.Id);
        await FollowupAsync(embed: embed);
    }
}