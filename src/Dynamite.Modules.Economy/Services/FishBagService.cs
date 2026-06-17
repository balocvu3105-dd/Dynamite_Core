// src/Dynamite.Modules.Economy/Services/FishBagService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public record BagSellResult(int FishSold, long CoinsEarned, int RemainingFish, long WalletBalance);

/// <summary>
/// Quản lý túi cá: xem, bán, nâng cấp dung lượng.
/// </summary>
public class FishBagService
{
    private const int MaxBagCapacity = 100;

    private readonly IFishBagRepository _bagRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ILogger<FishBagService> _logger;

    public FishBagService(
        IFishBagRepository bagRepo,
        IWalletRepository walletRepo,
        ILogger<FishBagService> logger)
    {
        _bagRepo   = bagRepo;
        _walletRepo = walletRepo;
        _logger    = logger;
    }

    public Task<UserFishBag> GetBagAsync(ulong guildId, ulong userId)
        => _bagRepo.GetOrCreateAsync(guildId, userId);

    // ── Sell ─────────────────────────────────────────────────────────────────

    /// <summary>Bán toàn bộ cá trong túi.</summary>
    public async Task<BagSellResult> SellAllAsync(ulong guildId, ulong userId)
    {
        var bag    = await _bagRepo.GetOrCreateAsync(guildId, userId);
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var fish   = bag.Fish.ToList();

        if (fish.Count == 0) return new BagSellResult(0, 0, 0, wallet.Coins);

        var total = fish.Sum(f => f.CoinValue);
        wallet.Coins += total;

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId    = guildId,
            ToWalletId = wallet.Id,
            Amount     = total,
            Type       = TransactionType.Fishing,
            Note       = $"Bán {fish.Count} con cá",
            CreatedAt  = DateTime.UtcNow
        });

        await _bagRepo.RemoveFishAsync(fish);
        await _bagRepo.SaveChangesAsync();
        await _walletRepo.SaveChangesAsync();

        _logger.LogInformation("User {UserId} sold {Count} fish for {Coins} coins", userId, fish.Count, total);
        return new BagSellResult(fish.Count, total, 0, wallet.Coins);
    }

    /// <summary>Bán theo rarity (ví dụ "Common", "Uncommon").</summary>
    public async Task<BagSellResult> SellByRarityAsync(ulong guildId, ulong userId, string rarity)
    {
        var bag    = await _bagRepo.GetOrCreateAsync(guildId, userId);
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var fish   = await _bagRepo.GetFishByRarityAsync(bag.Id, rarity);

        if (fish.Count == 0) return new BagSellResult(0, 0, bag.Fish.Count, wallet.Coins);

        var total = fish.Sum(f => f.CoinValue);
        wallet.Coins += total;

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId    = guildId,
            ToWalletId = wallet.Id,
            Amount     = total,
            Type       = TransactionType.Fishing,
            Note       = $"Bán {fish.Count} {rarity} fish",
            CreatedAt  = DateTime.UtcNow
        });

        await _bagRepo.RemoveFishAsync(fish);
        await _bagRepo.SaveChangesAsync();
        await _walletRepo.SaveChangesAsync();

        return new BagSellResult(fish.Count, total, bag.Fish.Count - fish.Count, wallet.Coins);
    }

    // ── Upgrade ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Nâng túi thêm <paramref name="slots"/> slot (thường là 10).
    /// Trả về (success, message, oldCap, newCap).
    /// </summary>
    public async Task<(bool success, string message, int oldCap, int newCap)> AddSlotsAsync(
        ulong guildId, ulong userId, int slots)
    {
        var bag = await _bagRepo.GetOrCreateAsync(guildId, userId);

        if (bag.BagCapacity >= MaxBagCapacity)
            return (false, $"Túi cá đã đạt tối đa **{MaxBagCapacity}** slot!", bag.BagCapacity, bag.BagCapacity);

        var oldCap = bag.BagCapacity;
        var newCap = Math.Min(oldCap + slots, MaxBagCapacity);

        bag.BagCapacity = newCap;
        await _bagRepo.SaveChangesAsync();

        _logger.LogInformation("User {UserId} expanded bag {Old} → {New} slots", userId, oldCap, newCap);
        return (true, string.Empty, oldCap, newCap);
    }

    public int GetMaxCapacity() => MaxBagCapacity;
}
