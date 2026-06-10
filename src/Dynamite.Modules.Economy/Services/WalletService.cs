// src/Dynamite.Modules.Economy/Services/WalletService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class WalletService
{
    private const long BaseDailyReward = 100;
    private const long StreakBonusPerDay = 10;
    private const long MaxStreakBonus = 200;

    private readonly IWalletRepository _repo;
    private readonly ILogger<WalletService> _logger;

    public WalletService(IWalletRepository repo, ILogger<WalletService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<(bool success, string message, long coins, int streak)> ClaimDailyAsync(
        ulong guildId, ulong userId)
    {
        var wallet = await _repo.GetOrCreateAsync(guildId, userId);
        var now = DateTime.UtcNow;

        if (wallet.LastDaily.HasValue)
        {
            var hoursSince = (now - wallet.LastDaily.Value).TotalHours;

            if (hoursSince < 24)
            {
                var next = wallet.LastDaily.Value.AddHours(24);
                var ts = new DateTimeOffset(next).ToUnixTimeSeconds();
                return (false, $"You already claimed today. Next daily: <t:{ts}:R>", 0, 0);
            }

            // Streak: nếu claim trong vòng 48h thì giữ streak, quá thì reset
            wallet.DailyStreak = hoursSince <= 48 ? wallet.DailyStreak + 1 : 1;
        }
        else
        {
            wallet.DailyStreak = 1;
        }

        var bonus = Math.Min((wallet.DailyStreak - 1) * StreakBonusPerDay, MaxStreakBonus);
        var earned = BaseDailyReward + bonus;

        wallet.Coins += earned;
        wallet.LastDaily = now;

        var tx = new Transaction
        {
            GuildId = guildId,
            ToWalletId = wallet.Id,
            Amount = earned,
            Type = TransactionType.Daily,
            Note = $"Daily reward (streak {wallet.DailyStreak})",
            CreatedAt = now
        };

        await _repo.AddTransactionAsync(tx);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("User {UserId} claimed daily: {Coins} coins (streak {Streak})",
            userId, earned, wallet.DailyStreak);

        return (true, string.Empty, earned, wallet.DailyStreak);
    }

    public async Task<UserWallet> GetWalletAsync(ulong guildId, ulong userId)
        => await _repo.GetOrCreateAsync(guildId, userId);

    public async Task<(bool success, string message)> TransferAsync(
        ulong guildId, ulong fromUserId, ulong toUserId, long amount)
    {
        if (fromUserId == toUserId)
            return (false, "You cannot transfer coins to yourself.");

        if (amount <= 0)
            return (false, "Amount must be greater than 0.");

        var from = await _repo.GetOrCreateAsync(guildId, fromUserId);
        var to = await _repo.GetOrCreateAsync(guildId, toUserId);

        if (from.Coins < amount)
            return (false, $"Insufficient balance. You have **{from.Coins:N0}** coins.");

        from.Coins -= amount;
        to.Coins += amount;

        var tx = new Transaction
        {
            GuildId = guildId,
            FromWalletId = from.Id,
            ToWalletId = to.Id,
            Amount = amount,
            Type = TransactionType.Transfer,
            Note = $"Transfer from {fromUserId} to {toUserId}",
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddTransactionAsync(tx);
        await _repo.SaveChangesAsync();

        return (true, $"✅ Transferred **{amount:N0}** coins to <@{toUserId}>.");
    }

    public async Task<List<(int rank, ulong userId, long coins)>> GetLeaderboardAsync(ulong guildId)
    {
        var wallets = await _repo.GetLeaderboardAsync(guildId);
        return wallets
            .Select((w, i) => (i + 1, w.UserId, w.Coins))
            .ToList();
    }
}