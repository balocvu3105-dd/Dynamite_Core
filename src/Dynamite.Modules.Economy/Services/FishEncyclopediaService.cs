// src/Dynamite.Modules.Economy/Services/FishEncyclopediaService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Quản lý Fish Encyclopedia — ghi nhận mỗi loài cá user câu được.
///
/// Design:
///   • Fire-and-forget từ FishingService / SpecialPoolService (wrapped try-catch)
///     → không ảnh hưởng response time của người câu
///   • Không ghi rác (Trash) và Miss/Escape vào encyclopedia
///   • Upsert theo (GuildId, UserId, FishName) — tăng TimesCaught, cập nhật BestCoins
/// </summary>
public class FishEncyclopediaService
{
    private static readonly HashSet<string> ExcludedRarities =
        ["Trash", "Miss", "Escape"];

    private readonly IFishEncyclopediaRepository _repo;
    private readonly ILogger<FishEncyclopediaService> _logger;

    public FishEncyclopediaService(
        IFishEncyclopediaRepository repo,
        ILogger<FishEncyclopediaService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>
    /// Ghi nhận 1 lần câu thành công vào encyclopedia.
    /// Bỏ qua Trash, Miss, Escape.
    /// Wraps exception — caller không bị ảnh hưởng nếu DB lỗi.
    /// </summary>
    public async Task RecordCatchAsync(
        ulong guildId, ulong userId,
        string fishName, string emoji, string rarity,
        long coins)
    {
        if (ExcludedRarities.Contains(rarity)) return;

        try
        {
            await _repo.UpsertAsync(guildId, userId, fishName, emoji, rarity, coins);
            await _repo.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "FishEncyclopedia: failed to record catch {Fish} for user {UserId}", fishName, userId);
        }
    }

    /// <summary>
    /// Lấy toàn bộ encyclopedia của user, sorted theo rarity order.
    /// </summary>
    public async Task<List<FishEncyclopediaEntry>> GetDexAsync(ulong guildId, ulong userId)
    {
        var entries = await _repo.GetUserDexAsync(guildId, userId);

        // Sort: Mythic → Legendary → Rare → Uncommon → Common → Chests → Trash
        return entries.OrderBy(e => RarityOrder(e.Rarity)).ThenBy(e => e.FishName).ToList();
    }

    /// <summary>Số loài unique đã câu.</summary>
    public Task<int> CountSpeciesAsync(ulong guildId, ulong userId)
        => _repo.CountSpeciesAsync(guildId, userId);

    private static int RarityOrder(string rarity) => rarity switch
    {
        "Mythic"    => 0,
        "Legendary" => 1,
        "Rare"      => 2,
        "Uncommon"  => 3,
        "Common"    => 4,
        "Diamond"   => 5,
        "Gold"      => 6,
        "Bronze"    => 7,
        _           => 99,  // Trash và unknown xuống cuối
    };
}
