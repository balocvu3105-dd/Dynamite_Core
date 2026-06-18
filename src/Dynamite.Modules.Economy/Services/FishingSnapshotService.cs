// src/Dynamite.Modules.Economy/Services/FishingSnapshotService.cs
namespace Dynamite.Modules.Economy.Services;

using System.Text.Json;
using Dynamite.Core.Common;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tạo và restore snapshot fishing data của user.
///
/// Trigger để tạo snapshot:
/// - Fishing level up           → reason "auto-levelup"
/// - Achievement unlock mới     → reason "auto-achievement"
/// - Mythic hoặc Pearl catch    → reason "auto-milestone"
/// - Weekly scheduler (Sunday)  → reason "auto-weekly"
/// - Admin manual               → reason "manual"
///
/// Mỗi user giữ tối đa 5 snapshots; cái cũ nhất bị xóa tự động.
/// Restore KHÔNG tự động — phải do admin ra lệnh để tránh nhầm.
/// </summary>
public class FishingSnapshotService
{
    private const int MaxSnapshotsPerUser = 5;

    private readonly IFishingSnapshotRepository _snapshotRepo;
    private readonly IUserProfileRepository     _profileRepo;
    private readonly IWalletRepository          _walletRepo;
    private readonly IFishBagRepository         _bagRepo;
    private readonly ILogger<FishingSnapshotService> _logger;

    public FishingSnapshotService(
        IFishingSnapshotRepository snapshotRepo,
        IUserProfileRepository     profileRepo,
        IWalletRepository          walletRepo,
        IFishBagRepository         bagRepo,
        ILogger<FishingSnapshotService> logger)
    {
        _snapshotRepo = snapshotRepo;
        _profileRepo  = profileRepo;
        _walletRepo   = walletRepo;
        _bagRepo      = bagRepo;
        _logger       = logger;
    }

    // ── Create snapshot ───────────────────────────────────────────────────────

    public async Task<FishingDataSnapshot> CreateSnapshotAsync(
        ulong guildId, ulong userId, string reason = "manual")
    {
        var profile = await _profileRepo.GetOrCreateFishingAsync(guildId, userId);
        var wallet  = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var bag     = await _bagRepo.GetOrCreateAsync(guildId, userId);

        // Serialize bag fish as lightweight JSON (name, rarity, coinValue only)
        var bagItems = bag.Fish.Select(f => new
        {
            f.FishName, f.FishEmoji, f.Rarity, f.CoinValue, f.IsSpecialCreature, f.IsPearl
        });
        var bagJson = JsonSerializer.Serialize(bagItems);

        // Collect achievement IDs
        var achieveIds = profile.Achievements.Any()
            ? string.Join(",", profile.Achievements.Select(a => a.AchievementId))
            : string.Empty;

        var snapshot = new FishingDataSnapshot
        {
            GuildId         = guildId,
            UserId          = userId,
            Reason          = reason,
            FishingXp       = profile.FishingXp,
            FishingLevel    = profile.FishingLevel,
            TotalCaught     = profile.TotalCaught,
            CommonCaught    = profile.CommonCaught,
            UncommonCaught  = profile.UncommonCaught,
            RareCaught      = profile.RareCaught,
            LegendaryCaught = profile.LegendaryCaught,
            MythicCaught    = profile.MythicCaught,
            ChestsOpened    = profile.ChestsOpened,
            WalletCoins     = wallet.Coins,
            BagSnapshotJson = bagJson,
            BagCapacity     = bag.BagCapacity,
            AchievementIds  = achieveIds,
            CreatedAt       = DateTime.UtcNow
        };

        await _snapshotRepo.AddAsync(snapshot);
        await _snapshotRepo.PruneExcessAsync(guildId, userId, MaxSnapshotsPerUser);
        await _snapshotRepo.SaveChangesAsync();

        _logger.LogInformation(
            "Fishing snapshot created for user {UserId} in guild {GuildId} — reason: {Reason}",
            userId, guildId, reason);

        return snapshot;
    }

    // ── Restore from snapshot ─────────────────────────────────────────────────

    public async Task<ServiceResult<string>> RestoreSnapshotAsync(
        ulong guildId, ulong userId, Guid snapshotId)
    {
        var snapshot = await _snapshotRepo.GetByIdAsync(snapshotId);
        if (snapshot is null || snapshot.GuildId != guildId || snapshot.UserId != userId)
            return ServiceResult<string>.Fail("Không tìm thấy snapshot này.");

        // Chụp snapshot hiện tại trước khi restore (safety net)
        await CreateSnapshotAsync(guildId, userId, $"pre-restore:{snapshotId:N}");

        // Restore fishing profile
        var profile = await _profileRepo.GetOrCreateFishingAsync(guildId, userId);
        profile.FishingXp       = snapshot.FishingXp;
        profile.FishingLevel    = snapshot.FishingLevel;
        profile.TotalCaught     = snapshot.TotalCaught;
        profile.CommonCaught    = snapshot.CommonCaught;
        profile.UncommonCaught  = snapshot.UncommonCaught;
        profile.RareCaught      = snapshot.RareCaught;
        profile.LegendaryCaught = snapshot.LegendaryCaught;
        profile.MythicCaught    = snapshot.MythicCaught;
        profile.ChestsOpened    = snapshot.ChestsOpened;

        // Restore wallet coins
        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        wallet.Coins = snapshot.WalletCoins;

        // Restore bag: xóa cá hiện tại và thêm lại từ snapshot
        var bag = await _bagRepo.GetOrCreateAsync(guildId, userId);
        if (bag.Fish.Count > 0)
            await _bagRepo.RemoveFishAsync(bag.Fish.ToList());

        bag.BagCapacity = snapshot.BagCapacity;

        if (!string.IsNullOrEmpty(snapshot.BagSnapshotJson))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<BagSnapshotItem>>(snapshot.BagSnapshotJson);
                if (items is not null)
                {
                    foreach (var item in items)
                    {
                        await _bagRepo.AddFishAsync(new CaughtFish
                        {
                            BagId     = bag.Id,
                            GuildId   = guildId,
                            UserId    = userId,
                            FishName  = item.FishName,
                            FishEmoji = item.FishEmoji,
                            Rarity    = item.Rarity,
                            CoinValue = item.CoinValue,
                            IsSpecialCreature = item.IsSpecialCreature,
                            IsPearl   = item.IsPearl,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not deserialize bag snapshot for user {UserId}", userId);
            }
        }

        await _profileRepo.SaveChangesAsync();
        await _walletRepo.SaveChangesAsync();
        await _bagRepo.SaveChangesAsync();

        _logger.LogWarning(
            "Fishing data restored for user {UserId} in guild {GuildId} from snapshot {SnapshotId}",
            userId, guildId, snapshotId);

        var snapshotTime = snapshot.CreatedAt.ToString("dd/MM/yyyy HH:mm");
        return ServiceResult<string>.Ok($"Đã restore dữ liệu câu cá từ **{snapshotTime} UTC** (lý do: {snapshot.Reason})");
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public Task<List<FishingDataSnapshot>> GetUserSnapshotsAsync(ulong guildId, ulong userId)
        => _snapshotRepo.GetUserSnapshotsAsync(guildId, userId);

    // ── Internal DTO ─────────────────────────────────────────────────────────

    private record BagSnapshotItem(
        string FishName, string FishEmoji, string Rarity,
        long CoinValue, bool IsSpecialCreature, bool IsPearl);
}
