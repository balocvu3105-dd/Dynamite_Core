// src/Dynamite.Modules.Economy/Services/FishBagService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public record BagSellResult(int FishSold, long CoinsEarned, int RemainingFish);

/// <summary>
/// Quản lý túi cá: xem, bán, nâng cấp dung lượng.
/// </summary>
public class FishBagService
{
    private const int MaxBagCapacity = 50;

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

        if (fish.Count == 0) return new BagSellResult(0, 0, 0);

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
        return new BagSellResult(fish.Count, total, 0);
    }

    /// <summary>Bán theo rarity (ví dụ "Common", "Uncommon").</summary>
    public async Task<BagSellResult> SellByRarityAsync(ulong guildId, ulong userId, string rarity)
    {
        var bag    = await _bagRepo.GetOrCreateAsync(guildId, userId);
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var fish   = await _bagRepo.GetFishByRarityAsync(bag.Id, rarity);

        if (fish.Count == 0) return new BagSellResult(0, 0, bag.Fish.Count);

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

        return new BagSellResult(fish.Count, total, bag.Fish.Count - fish.Count);
    }

    // ── Upgrade ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Nâng cấp túi khi mua BagUpgrade từ shop.
    /// targetCapacity từ InventoryItem.UsageCount (20 hoặc 50).
    /// </summary>
    public async Task<(bool success, string message)> UpgradeBagAsync(
        ulong guildId, ulong userId, int targetCapacity)
    {
        if (targetCapacity > MaxBagCapacity)
            return (false, $"Dung lượng tối đa là **{MaxBagCapacity}** slot.");

        var bag = await _bagRepo.GetOrCreateAsync(guildId, userId);

        if (bag.BagCapacity >= targetCapacity)
            return (false, $"Túi của bạn đã có **{bag.BagCapacity}** slot rồi!");

        bag.BagCapacity = targetCapacity;
        await _bagRepo.SaveChangesAsync();

        _logger.LogInformation("User {UserId} upgraded bag to {Cap} slots", userId, targetCapacity);
        return (true, $"✅ Túi cá đã được nâng cấp lên **{targetCapacity}** slot!");
    }
}
