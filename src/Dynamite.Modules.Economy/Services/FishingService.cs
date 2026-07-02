// src/Dynamite.Modules.Economy/Services/FishingService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Common;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Microsoft.Extensions.Logging;

public record FishResult(
    FishCatch Catch,
    long TotalCoins,
    string? RodName,
    PondWeather Weather,
    int PondRemaining,
    int FishingXpGained,
    LevelUpResult? FishingLevelUp,
    IReadOnlyList<AchievementUnlock> NewAchievements,
    bool SavedToBag,
    int BagFreeSlots,
    /// <summary>Độ bền còn lại sau lần câu này. null = rod không track durability.</summary>
    int? RodDurabilityLeft = null,
    /// <summary>true = lần câu này làm gãy cần câu.</summary>
    bool RodJustBroke = false,
    /// <summary>Tên khung giờ nếu đang có bonus (vd "🌅 Bình Minh"). null = không có bonus.</summary>
    string? TimeSlotName = null);

public record AchievementUnlock(string Id, string Title, string Description, long CoinReward);

/// <summary>
/// FishingService v2.2 — thêm Miss/Escape mechanic và activity logging.
///
/// Flow:
///   1. Cooldown check
///   2. Get rod stats (missRate, escapeRate, multiplier)
///   3. Roll → Miss? → set cooldown, log, return (pond KHÔNG bị trừ)
///   4. TryConsumeAsync (pond -1)
///   5. Roll outcome == Escape? → set cooldown, log, return (coins = 0)
///   6. Caught → coins, bag, XP, achievements, log
/// </summary>
public class FishingService
{
    private const int DefaultCooldownSeconds = 25;

    private readonly IWalletRepository       _walletRepo;
    private readonly IShopRepository         _shopRepo;
    private readonly IUserProfileRepository  _profileRepo;
    private readonly IFishBagRepository      _bagRepo;
    private readonly ILeaderboardRepository  _lbRepo;
    private readonly IFishingLogRepository   _fishLog;
    private readonly IFishTrophyRepository   _trophyRepo;
    private readonly IPondRepository _pondRepo;
    private readonly PondService    _pond;
    private readonly WeatherService _weather;
    private readonly XpService      _xp;
    private readonly FishEncyclopediaService _encyclopedia;
    private readonly ILogger<FishingService> _logger;

    public FishingService(
        IWalletRepository      walletRepo,
        IShopRepository        shopRepo,
        IUserProfileRepository profileRepo,
        IFishBagRepository     bagRepo,
        ILeaderboardRepository lbRepo,
        IFishingLogRepository  fishLog,
        IFishTrophyRepository  trophyRepo,
        IPondRepository pondRepo,
        PondService    pond,
        WeatherService weather,
        XpService      xp,
        FishEncyclopediaService encyclopedia,
        ILogger<FishingService> logger)
    {
        _walletRepo   = walletRepo;
        _shopRepo     = shopRepo;
        _profileRepo  = profileRepo;
        _bagRepo      = bagRepo;
        _lbRepo       = lbRepo;
        _fishLog      = fishLog;
        _trophyRepo   = trophyRepo;
        _pondRepo     = pondRepo;
        _pond         = pond;
        _encyclopedia = encyclopedia;
        _weather     = weather;
        _xp          = xp;
        _logger      = logger;
    }

    public async Task<ServiceResult<FishResult>>
        FishAsync(ulong guildId, ulong userId, bool useBait = true)
    {
        // ── 1. Load profile + wallet + rod ───────────────────────────────────
        var profile = await _profileRepo.GetOrCreateFishingAsync(guildId, userId);
        var wallet  = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var bestRod = await _shopRepo.GetBestRodAsync(wallet.Id);

        // ── 2. Cooldown check ────────────────────────────────────────────────
        var cooldownSec = bestRod?.Item.CooldownSeconds ?? DefaultCooldownSeconds;
        if (profile.LastFishedAt.HasValue)
        {
            var elapsed = (DateTime.UtcNow - profile.LastFishedAt.Value).TotalSeconds;
            if (elapsed < cooldownSec)
            {
                var wait = (int)(cooldownSec - elapsed);
                return ServiceResult<FishResult>.Fail($"⏳ Chờ **{wait}s** nữa mới câu được!");
            }
        }

        // ── 3. Rod stats + Weather ────────────────────────────────────────────
        var missRate    = bestRod?.Item.MissRate   ?? FishingDropTable.DefaultMissRate;
        var escapeRate  = bestRod?.Item.EscapeRate ?? FishingDropTable.DefaultEscapeRate;
        var multiplier  = bestRod?.Item.DropMultiplier ?? 1.0;

        // Guild owner override (từ /set-fish-rate) — ghi đè rod rate nếu được set
        var pond = await _pondRepo.GetOrCreateAsync(guildId);
        if (pond.FishMissRateOverride.HasValue)   missRate   = pond.FishMissRateOverride.Value;
        if (pond.FishEscapeRateOverride.HasValue)  escapeRate = pond.FishEscapeRateOverride.Value;

        var currentWeather = await _weather.GetCurrentWeatherAsync(guildId);
        var (rareMod, legendaryMod, _, missMod, coinMod) = WeatherService.GetModifiers(currentWeather);

        // ── 4. Bait check ─────────────────────────────────────────────────────
        // useBait = false khi auto-fish và user không chọn dùng bait
        var baitMod  = 0.0;
        if (useBait)
        {
            var baitItem = await GetActiveBaitAsync(wallet.Id);
            if (baitItem is not null)
            {
                baitMod = 0.10; // +10% Rare
                await ConsumeBaitChargeAsync(baitItem);
            }
        }

        // ── 5. Roll (miss check ở đây — TRƯỚC khi trừ pond) ─────────────────
        var luckBonus = bestRod?.Item.LuckBonus ?? 0;
        var (timeDawnMod, timeNightMod) = FishingDropTable.GetTimeOfDayModifier();

        var roll = FishingDropTable.Roll(
            missRate:       missRate,
            escapeRate:     escapeRate,
            dropMultiplier: multiplier,
            rareMod:        rareMod + baitMod + timeDawnMod,
            legendaryMod:   legendaryMod + timeNightMod,
            missMod:        missMod,
            luckBonus:      luckBonus);

        profile.LastFishedAt = DateTime.UtcNow;

        if (roll.Outcome == RollOutcome.Miss)
        {
            await LogFishEventAsync(guildId, userId,
                FishingEvent.Miss, null, bestRod?.Item.Name, currentWeather, -1);
            await _profileRepo.SaveChangesAsync();
            return ServiceResult<FishResult>.Fail("🎣 **Hụt!** Không có gì cắn câu lần này... 👉😄 lêu lêu~");
        }

        // ── 6. Pond consume (chỉ khi không miss) ─────────────────────────────
        var pondResult = await _pond.TryConsumeAsync(guildId);
        if (!pondResult) return ServiceResult<FishResult>.Fail(pondResult.ErrorMessage);

        // ── 7. Escape ────────────────────────────────────────────────────────
        if (roll.Outcome == RollOutcome.Escape)
        {
            // roll.Fish CAN be null for chest escapes (FishingDropTable line 87 returns null fish).
            var escaped = roll.Fish;
            await LogFishEventAsync(guildId, userId,
                FishingEvent.Escape, escaped, bestRod?.Item.Name, currentWeather,
                pondResult.Value?.CurrentFish ?? -1);
            await _profileRepo.SaveChangesAsync();
            return ServiceResult<FishResult>.Fail(
                escaped is null
                    ? "😱 **Hòm Kho Báu** vừa tuột khỏi câu! Luyện thêm nhé."
                    : $"😱 **{escaped.Name}** ({escaped.Rarity}) cắn câu rồi thoát mất! Luyện thêm nhé.");
        }

        // ── 8. Caught: track stats ────────────────────────────────────────────
        // roll.Fish should always be non-null for Caught, but guard defensively.
        if (roll.Fish is null)
            return ServiceResult<FishResult>.Fail("⚠️ Lỗi nội bộ: không xác định được cá câu được.");

        // Áp dụng coinMod từ weather (Stormy ×1.25 → cá đáng tiền hơn vì nguy hiểm)
        var fishCatch = coinMod == 1.0
            ? roll.Fish
            : roll.Fish with { Coins = (long)(roll.Fish.Coins * coinMod) };
        profile.TotalCaught++;
        if (!fishCatch.IsChest)
        {
            _ = fishCatch.Rarity switch
            {
                "Common"    => profile.CommonCaught++,
                "Uncommon"  => profile.UncommonCaught++,
                "Rare"      => profile.RareCaught++,
                "Legendary" => profile.LegendaryCaught++,
                "Mythic"    => profile.MythicCaught++,
                _           => 0
            };
        }
        else
            profile.ChestsOpened++;

        // ── 9. Coins ──────────────────────────────────────────────────────────
        // Coins KHÔNG được cộng khi câu — chỉ nhận khi bán cá qua /bag sell.
        // wallet vẫn cần cho achievement reward (TryAward bên dưới cộng vào wallet).

        // ── 10. Fish Bag ──────────────────────────────────────────────────────
        var bag        = await _bagRepo.GetOrCreateAsync(guildId, userId);
        var savedToBag = false;

        if (!bag.IsFull)
        {
            await _bagRepo.AddFishAsync(new CaughtFish
            {
                BagId     = bag.Id,
                GuildId   = guildId,
                UserId    = userId,
                FishName  = fishCatch.Name,
                FishEmoji = fishCatch.Emoji,
                Rarity    = fishCatch.Rarity,
                CoinValue = fishCatch.Coins,
                IsPearl   = fishCatch.Name is "Viên Ngọc Đại Dương",
                CreatedAt = DateTime.UtcNow
            });
            savedToBag = true;
        }

        // ── 11. Weekly activity ───────────────────────────────────────────────
        var weeklyActivity = await _lbRepo.GetOrCreateWeeklyActivityAsync(guildId, userId);
        weeklyActivity.WeeklyFishCaught++;

        // ── 12. Fishing XP ────────────────────────────────────────────────────
        var xpGained      = XpService.FishingXpTable.GetValueOrDefault(fishCatch.Rarity, 20);
        var levelUpResult = await _xp.AwardFishingXpAsync(guildId, userId, fishCatch.Rarity, profile);

        // ── 13. Achievements ──────────────────────────────────────────────────
        var newAchievements = await CheckAndAwardAchievementsAsync(guildId, userId, profile, wallet, fishCatch);

        // ── 14. Trophy (Rare+ → ghi vào bộ sưu tầm, mỗi loài 1 lần) ──────────
        if (fishCatch.Rarity is "Rare" or "Legendary" or "Mythic")
            await _trophyRepo.TryAddAsync(guildId, userId,
                fishCatch.Name, fishCatch.Rarity,
                isPearl: fishCatch.Name is "Viên Ngọc Đại Dương");

        // ── 15. Activity log (chỉ log sự kiện đáng chú ý) ───────────────────
        // Bỏ qua Common/Uncommon Caught → giảm ~70% DB writes cho fishing log.
        // Vẫn log: BagFull, Pearl, Rare+ Caught, Miss (step 5), Escape (step 7).
        var logEvent = fishCatch.Name is "Viên Ngọc Đại Dương"
            ? FishingEvent.PearlCaught
            : savedToBag ? FishingEvent.Caught : FishingEvent.BagFull;

        var shouldLog = logEvent != FishingEvent.Caught
            || fishCatch.Rarity is "Rare" or "Legendary" or "Mythic";

        if (shouldLog)
            await LogFishEventAsync(guildId, userId, logEvent, fishCatch,
                bestRod?.Item.Name, currentWeather, pondResult.Value?.CurrentFish ?? -1,
                coinsEarned: fishCatch.Coins, xpEarned: xpGained);

        // ── 15. Rod Durability — trừ 1 sau mỗi lần catch thành công ────────────
        int? rodDurabilityLeft = null;
        bool rodJustBroke      = false;

        if (bestRod is not null && bestRod.RodDurability.HasValue)
        {
            bestRod.RodDurability -= 1;
            rodDurabilityLeft      = bestRod.RodDurability;
            rodJustBroke           = bestRod.RodDurability <= 0;
            if (rodJustBroke) bestRod.RodDurability = 0;
        }

        // ── 16. Save — tất cả repos share cùng DbContext (Scoped), 1 call là đủ ──
        await _walletRepo.SaveChangesAsync();

        _logger.LogDebug("User {UserId} fished: {Fish} ({Rarity}) = {Coins}c | Bag:{Saved} | Pond:{Remaining}",
            userId, fishCatch.Name, fishCatch.Rarity, fishCatch.Coins,
            savedToBag ? "saved" : "dropped", pondResult.Value?.CurrentFish ?? -1);

        // ── 17. Encyclopedia — fire-and-forget, không block response ─────────
        _ = _encyclopedia.RecordCatchAsync(
            guildId, userId,
            fishCatch.Name, fishCatch.Emoji, fishCatch.Rarity, fishCatch.Coins);

        return ServiceResult<FishResult>.Ok(new FishResult(
            Catch:           fishCatch,
            TotalCoins:      wallet.Coins,
            RodName:         bestRod?.Item.Name ?? "🎋 Cần Câu Tân Thủ",
            Weather:         currentWeather,
            PondRemaining:   pondResult.Value?.CurrentFish ?? -1,
            FishingXpGained: xpGained,
            FishingLevelUp:  levelUpResult,
            NewAchievements:   newAchievements,
            SavedToBag:        savedToBag,
            BagFreeSlots:      bag.IsFull ? 0 : bag.FreeSlots - 1,
            RodDurabilityLeft: rodDurabilityLeft,
            RodJustBroke:      rodJustBroke,
            TimeSlotName:      FishingDropTable.GetTimeSlotName()));
    }

    // ── Bait helpers ──────────────────────────────────────────────────────────

    private Task<UserInventory?> GetActiveBaitAsync(Guid walletId)
        => _shopRepo.GetActiveBaitAsync(walletId);

    private async Task ConsumeBaitChargeAsync(UserInventory bait)
    {
        bait.Quantity--;
        if (bait.Quantity <= 0)
            await _shopRepo.RemoveUserInventoryAsync(bait);
    }

    // ── Activity log helper ───────────────────────────────────────────────────

    private Task LogFishEventAsync(
        ulong guildId, ulong userId,
        FishingEvent evt, FishCatch? fish,
        string? rodName, PondWeather weather, int pondRemaining,
        long coinsEarned = 0, int xpEarned = 0, string? poolName = null)
    {
        return _fishLog.AddAsync(new FishingActivityLog
        {
            GuildId       = guildId,
            UserId        = userId,
            Event         = evt,
            FishName      = fish?.Name,
            Rarity        = fish?.Rarity,
            CoinsEarned   = coinsEarned,
            XpEarned      = xpEarned,
            PoolName      = poolName,
            RodName       = rodName,
            Weather       = weather.ToString(),
            PondRemaining = pondRemaining,
            CreatedAt     = DateTime.UtcNow
        });
    }

    // ── Achievement checker ───────────────────────────────────────────────────

    private async Task<List<AchievementUnlock>> CheckAndAwardAchievementsAsync(
        ulong guildId, ulong userId,
        UserFishingProfile profile, UserWallet wallet, FishCatch fishCatch)
    {
        var unlocked = new List<AchievementUnlock>();

        // Dùng profile.Achievements đã được Include() sẵn — KHÔNG query DB thêm.
        // Tránh N+1: cũ = 9+ AnyAsync() riêng lẻ; mới = 0 extra DB calls.
        var earned = profile.Achievements.Select(a => a.AchievementId).ToHashSet();

        async Task TryAward(string id, string title, string desc, long reward)
        {
            if (earned.Contains(id)) return;
            var ach = new UserFishingAchievement { GuildId = guildId, UserId = userId, AchievementId = id };
            await _profileRepo.AddAchievementAsync(ach);
            profile.Achievements.Add(ach); // giữ in-memory set đồng bộ
            earned.Add(id);
            wallet.Coins += reward;
            unlocked.Add(new AchievementUnlock(id, title, desc, reward));
        }

        if (profile.TotalCaught == 1)
            await TryAward(AchievementIds.FirstCatch, "🎣 Tân Binh", "Câu được con cá đầu tiên!", 50);
        if (fishCatch.IsChest && profile.ChestsOpened == 1)
            await TryAward(AchievementIds.FirstChest, "📦 Kho Báu", "Mở hòm lần đầu!", 200);

        if (profile.CommonCaught    >= 50)  await TryAward(AchievementIds.Catch50Common,    "🐟 Ngư Dân",    "Câu 50 Cá Thường!",       300);
        if (profile.UncommonCaught  >= 50)  await TryAward(AchievementIds.Catch50Uncommon,  "🐠 Thợ Câu",    "Câu 50 Cá Hiếm Vừa!",     600);
        if (profile.RareCaught      >= 50)  await TryAward(AchievementIds.Catch50Rare,      "🐡 Săn Hiếm",   "Câu 50 Cá Hiếm!",         1000);
        if (profile.LegendaryCaught >= 50)  await TryAward(AchievementIds.Catch50Legendary, "🦈 Huyền Thoại","Câu 50 Cá Huyền Thoại!",  3000);
        if (profile.MythicCaught    >= 10)  await TryAward(AchievementIds.Catch10Mythic,    "🐉 Thần Câu",   "Câu 10 Cá Thần!",         5000);
        if (profile.TotalCaught     >= 500) await TryAward(AchievementIds.Catch500Total,    "🏅 Ngư Ông",    "Câu tổng 500 con!",        2000);
        if (profile.TotalCaught     >= 1000)await TryAward(AchievementIds.Catch1000Total,   "🏆 Ngư Vương",  "Câu tổng 1000 con!",       5000);

        if (fishCatch.IsChest && fishCatch.Rarity == "Gold")
            await TryAward(AchievementIds.OpenGoldChest,    "🪙 Vàng!",       "Mở Hòm Vàng lần đầu!",       500);
        if (fishCatch.IsChest && fishCatch.Rarity == "Diamond")
            await TryAward(AchievementIds.OpenDiamondChest, "💎 Kim Cương!",  "Mở Hòm Kim Cương lần đầu!", 2000);

        if (profile.FishingLevel >= 10)  await TryAward(AchievementIds.FishingLevel10,  "⭐ Cấp 10",  "Đạt Fishing Level 10!",  500);
        if (profile.FishingLevel >= 50)  await TryAward(AchievementIds.FishingLevel50,  "🌟 Cấp 50",  "Đạt Fishing Level 50!",  3000);
        if (profile.FishingLevel >= 100) await TryAward(AchievementIds.FishingLevel100, "💫 Cấp 100", "Đạt Fishing Level 100!", 10000);

        return unlocked;
    }
}
