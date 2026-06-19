// src/Dynamite.Modules.Economy/Services/FishBagService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Common;
using Dynamite.Core.Common.Results;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public record BagSellResult(
    int  FishSold,
    long CoinsEarned,
    int  RemainingFish,
    long WalletBalance,
    /// <summary>
    /// Hệ số giá thị trường tại thời điểm bán.
    /// 1.0 = bình thường · >1.0 = bể cạn (giá cao) · <1.0 = bể đầy (giá thấp).
    /// </summary>
    double MarketMultiplier = 1.0);

/// <summary>
/// Quản lý túi cá: xem, bán, nâng cấp dung lượng.
///
/// Fish Market mechanic:
///   Giá bán dao động theo lượng cá trong bể guild.
///   Công thức: multiplier = 0.6 + (1 − fillRatio) × 1.4
///     fillRatio = CurrentFish / MaxFish
///     Bể đầy (fill=1.0) → ×0.6  (nguồn cung dồi dào, giá thấp)
///     Bể trống (fill=0) → ×2.0  (khan hiếm, giá cao)
///     Bể 50%   (fill=0.5) → ×1.3
/// </summary>
public class FishBagService
{
    private const int MaxBagCapacity = 100;

    private readonly IFishBagRepository _bagRepo;
    private readonly IWalletRepository  _walletRepo;
    private readonly IPondRepository    _pondRepo;
    private readonly ILogger<FishBagService> _logger;

    public FishBagService(
        IFishBagRepository bagRepo,
        IWalletRepository  walletRepo,
        IPondRepository    pondRepo,
        ILogger<FishBagService> logger)
    {
        _bagRepo    = bagRepo;
        _walletRepo = walletRepo;
        _pondRepo   = pondRepo;
        _logger     = logger;
    }

    public Task<UserFishBag> GetBagAsync(ulong guildId, ulong userId)
        => _bagRepo.GetOrCreateAsync(guildId, userId);

    // ── Market multiplier ─────────────────────────────────────────────────────

    private async Task<double> GetMarketMultiplierAsync(ulong guildId)
    {
        var pond = await _pondRepo.GetOrCreateAsync(guildId);
        if (pond.MaxFish <= 0) return 1.0;

        var fillRatio  = Math.Clamp((double)pond.CurrentFish / pond.MaxFish, 0.0, 1.0);
        var multiplier = 0.6 + (1.0 - fillRatio) * 1.4;
        return Math.Round(multiplier, 2);
    }

    // ── Sell ─────────────────────────────────────────────────────────────────

    /// <summary>Bán toàn bộ cá trong túi với giá thị trường hiện tại.</summary>
    public async Task<BagSellResult> SellAllAsync(ulong guildId, ulong userId)
    {
        var bag    = await _bagRepo.GetOrCreateAsync(guildId, userId);
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var fish   = bag.Fish.ToList();

        if (fish.Count == 0) return new BagSellResult(0, 0, 0, wallet.Coins);

        var multiplier = await GetMarketMultiplierAsync(guildId);
        var total      = (long)(fish.Sum(f => f.CoinValue) * multiplier);
        wallet.Coins  += total;

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId    = guildId,
            ToWalletId = wallet.Id,
            Amount     = total,
            Type       = TransactionType.Fishing,
            Note       = $"Bán {fish.Count} con cá (×{multiplier:F2})",
            CreatedAt  = DateTime.UtcNow
        });

        await _bagRepo.RemoveFishAsync(fish);
        await _bagRepo.SaveChangesAsync();
        await _walletRepo.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} sold {Count} fish for {Coins} coins (market ×{Mult:F2})",
            userId, fish.Count, total, multiplier);

        return new BagSellResult(fish.Count, total, 0, wallet.Coins, multiplier);
    }

    /// <summary>Bán theo rarity với giá thị trường hiện tại.</summary>
    public async Task<BagSellResult> SellByRarityAsync(ulong guildId, ulong userId, string rarity)
    {
        var bag    = await _bagRepo.GetOrCreateAsync(guildId, userId);
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var fish   = await _bagRepo.GetFishByRarityAsync(bag.Id, rarity);

        if (fish.Count == 0) return new BagSellResult(0, 0, bag.Fish.Count, wallet.Coins);

        var multiplier = await GetMarketMultiplierAsync(guildId);
        var total      = (long)(fish.Sum(f => f.CoinValue) * multiplier);
        wallet.Coins  += total;

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId    = guildId,
            ToWalletId = wallet.Id,
            Amount     = total,
            Type       = TransactionType.Fishing,
            Note       = $"Bán {fish.Count} {rarity} fish (×{multiplier:F2})",
            CreatedAt  = DateTime.UtcNow
        });

        await _bagRepo.RemoveFishAsync(fish);
        await _bagRepo.SaveChangesAsync();
        await _walletRepo.SaveChangesAsync();

        return new BagSellResult(fish.Count, total, bag.Fish.Count - fish.Count, wallet.Coins, multiplier);
    }

    // ── Upgrade ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Nâng túi thêm <paramref name="slots"/> slot (thường là 10).
    /// </summary>
    public async Task<ServiceResult<BagUpgradeResult>> AddSlotsAsync(
        ulong guildId, ulong userId, int slots)
    {
        var bag = await _bagRepo.GetOrCreateAsync(guildId, userId);

        if (bag.BagCapacity >= MaxBagCapacity)
            return ServiceResult<BagUpgradeResult>.Fail($"Túi cá đã đạt tối đa **{MaxBagCapacity}** slot!");

        var oldCap = bag.BagCapacity;
        var newCap = Math.Min(oldCap + slots, MaxBagCapacity);

        bag.BagCapacity = newCap;
        await _bagRepo.SaveChangesAsync();

        _logger.LogInformation("User {UserId} expanded bag {Old} → {New} slots", userId, oldCap, newCap);
        return ServiceResult<BagUpgradeResult>.Ok(new BagUpgradeResult(oldCap, newCap, 0));
    }

    public int GetMaxCapacity() => MaxBagCapacity;
}
