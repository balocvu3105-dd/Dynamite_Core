// src/Dynamite.Modules.Economy/Services/SpecialPoolService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Common;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Microsoft.Extensions.Logging;

public record SpecialFishResult(
    SpecialFishingDropTable.SpecialFishCatch Catch,
    long     TotalCoins,
    int      PondRemaining,
    int      FishingXpGained,
    LevelUpResult? FishingLevelUp,
    bool     SavedToBag,
    int      BagFreeSlots,
    bool     PearlCapReached);   // true → pearl rolled, cap già đầy → không nhận

/// <summary>
/// Xử lý việc câu cá tại Special Pool:
/// - Level 20+ check
/// - Pearl guild-wide cap (3 per 7 days)
/// - Lưu cá vào túi / drop nếu đầy
/// - Cập nhật weekly activity
/// </summary>
public class SpecialPoolService
{
    private const int PearlGuildWeeklyCap = 3;
    private static readonly TimeSpan PearlWindow = TimeSpan.FromDays(7);

    private readonly ISpecialPoolRepository _poolRepo;
    private readonly IUserProfileRepository _profileRepo;
    private readonly IWalletRepository      _walletRepo;
    private readonly IShopRepository        _shopRepo;
    private readonly IFishBagRepository     _bagRepo;
    private readonly ILeaderboardRepository _lbRepo;
    private readonly XpService              _xp;
    private readonly ILogger<SpecialPoolService> _logger;

    public SpecialPoolService(
        ISpecialPoolRepository poolRepo,
        IUserProfileRepository profileRepo,
        IWalletRepository      walletRepo,
        IShopRepository        shopRepo,
        IFishBagRepository     bagRepo,
        ILeaderboardRepository lbRepo,
        XpService              xp,
        ILogger<SpecialPoolService> logger)
    {
        _poolRepo    = poolRepo;
        _profileRepo = profileRepo;
        _walletRepo  = walletRepo;
        _shopRepo    = shopRepo;
        _bagRepo     = bagRepo;
        _lbRepo      = lbRepo;
        _xp          = xp;
        _logger      = logger;
    }

    // ── Public: get available pools ───────────────────────────────────────────

    public Task<List<SpecialPool>> GetActivePoolsAsync(ulong guildId)
        => _poolRepo.GetActivePoolsAsync(guildId);

    // ── Public: fish in a specific pool ──────────────────────────────────────

    public async Task<ServiceResult<SpecialFishResult>>
        FishSpecialAsync(ulong guildId, ulong userId, Guid poolId)
    {
        // 1. Load pool
        var pool = await _poolRepo.GetByIdAsync(poolId);
        if (pool is null || !pool.IsActive)
            return ServiceResult<SpecialFishResult>.Fail("Pool này không còn hoạt động.");

        // 2. Level check
        var profile = await _profileRepo.GetOrCreateFishingAsync(guildId, userId);
        if (profile.FishingLevel < pool.MinLevel)
            return ServiceResult<SpecialFishResult>.Fail(
                $"Cần **Fishing Level {pool.MinLevel}** để câu tại pool này. " +
                $"Bạn đang ở Level **{profile.FishingLevel}**.");

        // 3. Load wallet (dùng cho TotalCoins trong result)
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);

        // 4. Tính weak-rod penalty: Cần Câu Tre/Bạc có tỉ lệ ra rác cao trong special pool.
        //    Mechanic này khuyến khích upgrade lên Cần Câu Vàng+ trước khi dùng vé pool.
        //    Cần Câu Tre  (MissRate 0.13) → 20% trash
        //    Cần Câu Bạc  (MissRate 0.11) → 10% trash
        //    Cần Câu Vàng+ (MissRate < 0.11) → 0% penalty
        var bestRod = await _shopRepo.GetBestRodAsync(wallet.Id);
        var weakRodTrashRate = bestRod?.Item.Name switch
        {
            "Cần Câu Tre"  => 0.20,
            "Cần Câu Bạc"  => 0.10,
            _              => 0.0
        };

        // 5. Roll drop table (với weak-rod penalty nếu có)
        var catch_ = SpecialFishingDropTable.Roll(pool.DropTable, weakRodTrashRate);

        // Nếu ra rác (weak-rod penalty): pool slot vẫn bị tiêu, nhưng không lưu vào túi,
        // không tính XP, không tạm dừng session.
        if (catch_.Rarity == "Trash")
        {
            pool.RemainingFish--;
            await _poolRepo.SaveChangesAsync();

            _logger.LogDebug(
                "[SpecialPool] Weak-rod trash for user {UserId} (rod: {Rod}, trashRate: {Rate:P0})",
                userId, bestRod?.Item.Name ?? "none", weakRodTrashRate);

            return ServiceResult<SpecialFishResult>.Ok(new SpecialFishResult(
                Catch:           catch_,
                TotalCoins:      wallet.Coins,
                PondRemaining:   pool.RemainingFish,
                FishingXpGained: 0,
                FishingLevelUp:  null,
                SavedToBag:      true,  // true = không trigger bag-full pause
                BagFreeSlots:    0,
                PearlCapReached: false));
        }

        // 6. Pearl cap enforcement
        var pearlCapReached = false;
        if (catch_.IsPearl)
        {
            var since       = DateTime.UtcNow - PearlWindow;
            var pearlCount  = await _poolRepo.GetGuildPearlCountAsync(guildId, since);

            if (pearlCount >= PearlGuildWeeklyCap)
            {
                // Đủ hạn mức → pearl biến thành cá thường thay thế
                pearlCapReached = true;
                catch_ = new SpecialFishingDropTable.SpecialFishCatch(
                    "🦑 Mực Khổng Lồ", "🦑", 280, "Uncommon");
                _logger.LogInformation(
                    "Pearl cap reached for guild {GuildId} — replaced pearl with consolation catch", guildId);
            }
            else
            {
                await _poolRepo.AddPearlLogAsync(new GuildPearlLog
                {
                    GuildId   = guildId,
                    UserId    = userId,
                    PearlType = PearlType.SeaEye,
                    CreatedAt = DateTime.UtcNow
                });
                _logger.LogWarning(
                    "🎉 User {UserId} caught Con Mắt Biển Cả in guild {GuildId}! Count: {Count}/{Cap}",
                    userId, guildId, pearlCount + 1, PearlGuildWeeklyCap);
            }
        }

        // 7. Consume 1 fish from pool
        pool.RemainingFish--;

        // 8. Coins — KHÔNG cộng khi câu, chỉ nhận khi bán qua /bag sell.
        // wallet vẫn cần cho achievement/XP reward bên dưới.

        // 9. Fish bag
        var bag        = await _bagRepo.GetOrCreateAsync(guildId, userId);
        var savedToBag = false;

        if (!bag.IsFull)
        {
            await _bagRepo.AddFishAsync(new CaughtFish
            {
                BagId      = bag.Id,
                GuildId    = guildId,
                UserId     = userId,
                FishName   = catch_.Name,
                FishEmoji  = catch_.Emoji,
                Rarity     = catch_.Rarity,
                CoinValue  = catch_.Coins,
                SourcePool = pool.PoolName,
                IsSpecialCreature = true,
                IsPearl    = catch_.IsPearl,
                CreatedAt  = DateTime.UtcNow
            });
            savedToBag = true;
        }

        // 10. Weekly activity
        var activity = await _lbRepo.GetOrCreateWeeklyActivityAsync(guildId, userId);
        activity.WeeklyFishCaught++;

        // 11. Fishing XP
        var xpGained    = XpService.FishingXpTable.GetValueOrDefault(catch_.Rarity, 20);
        var levelUpResult = await _xp.AwardFishingXpAsync(guildId, userId, catch_.Rarity, profile);

        // 12. Save
        await _walletRepo.SaveChangesAsync();
        await _poolRepo.SaveChangesAsync();
        await _bagRepo.SaveChangesAsync();
        await _lbRepo.SaveChangesAsync();
        await _shopRepo.SaveChangesAsync();

        // Profile được XpService save nội bộ (hoặc save cùng profileRepo)
        // Gọi thêm để chắc chắn cooldown / stats được lưu nếu có
        await _profileRepo.SaveChangesAsync();

        var fishResult = new SpecialFishResult(
            Catch:          catch_,
            TotalCoins:     wallet.Coins,
            PondRemaining:  pool.RemainingFish,
            FishingXpGained: xpGained,
            FishingLevelUp: levelUpResult,
            SavedToBag:     savedToBag,
            BagFreeSlots:   savedToBag ? bag.FreeSlots - 1 : 0,
            PearlCapReached: pearlCapReached);

        return ServiceResult<SpecialFishResult>.Ok(fishResult);
    }
}
