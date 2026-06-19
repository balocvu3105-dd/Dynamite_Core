// src/Dynamite.Modules.Economy/Helpers/SpecialFishingDropTable.cs
namespace Dynamite.Modules.Economy.Helpers;

using Dynamite.Core.Entities;

/// <summary>
/// Drop table cho Special Pool (level 20+).
/// Mỗi SpecialDropTable enum có bảng giống loài riêng.
/// Pearl "Con Mắt Biển Cả" được check cap ở SpecialPoolService,
/// không nằm trong weight table — được roll riêng với xác suất 0.0001%.
///
/// Balancing (v2):
///   • Tất cả giá cá giảm 50% so với v1
///   • Legendary/Mythic weight giảm ~40%, redistribute lên Rare/Uncommon
///   • Thêm miss/escape per-pool — pool khó hơn thì hụt nhiều hơn
///   • Miss KHÔNG tiêu pool slot, Escape có tiêu
/// </summary>
public static class SpecialFishingDropTable
{
    public record SpecialFishCatch(
        string Name,
        string Emoji,
        long   Coins,
        string Rarity,
        bool   IsPearl = false,
        bool   IsMiss  = false,
        bool   IsEscape = false);

    // ── Special outcomes ──────────────────────────────────────────────────────
    public static readonly SpecialFishCatch Miss   = new("Miss",   "💨", 0, "Miss",  IsMiss:   true);
    public static readonly SpecialFishCatch Escape = new("Escape", "🌊", 0, "Escape", IsEscape: true);

    // ── Per-pool miss/escape rates ────────────────────────────────────────────
    // Pool khó hơn → hụt nhiều hơn. Miss: cá không cắn (không tiêu slot).
    // Escape: cá cắn nhưng thoát (tiêu slot, không có xu).
    public static (double MissRate, double EscapeRate) GetPoolRates(SpecialDropTable table) => table switch
    {
        SpecialDropTable.CoralBay       => (0.05, 0.03),
        SpecialDropTable.MangroveForest => (0.08, 0.05),
        SpecialDropTable.DeepOcean      => (0.12, 0.07),
        SpecialDropTable.AbyssalZone    => (0.18, 0.10),
        _                               => (0.05, 0.03)
    };

    // ── Creatures per pool ────────────────────────────────────────────────────
    // Giá đã giảm 50% so với v1.
    // Weight Legendary/Mythic giảm ~40%, phần dư cộng vào Rare/Uncommon.

    private static readonly (SpecialFishCatch Fish, int Weight)[] CoralBayTable =
    [
        (new("🦞 Tôm Hùm",        "🦞",  75,  "Common"),    40),
        (new("🦑 Mực Khổng Lồ",   "🦑", 140,  "Uncommon"),  31),
        (new("🐙 Bạch Tuột",       "🐙", 175,  "Uncommon"),  19),
        (new("🪼 Sứa Phát Quang",  "🪼", 350,  "Rare"),       8), // +1 từ L giảm
        (new("🦈 Cá Mập Trắng",    "🦈", 1250, "Legendary"),  2), // 3→2
    ];

    private static readonly (SpecialFishCatch Fish, int Weight)[] DeepOceanTable =
    [
        (new("🐙 Bạch Tuột Khổng Lồ", "🐙",  250, "Uncommon"),  38), // +3
        (new("🦑 Mực Ma Quái",         "🦑",  300, "Uncommon"),  27), // +2
        (new("🪼 Sứa Xanh Điện",       "🪼",  600, "Rare"),      23), // +3
        (new("🦈 Cá Mập Trắng Lớn",    "🦈", 2000, "Legendary"),  9), // 15→9
        (new("🐋 Cá Voi Xanh",         "🐋", 4000, "Mythic"),     3), // 5→3
    ];

    private static readonly (SpecialFishCatch Fish, int Weight)[] MangroveForestTable =
    [
        (new("🦐 Tôm Nước Ngọt",    "🦐",   50, "Common"),   47), // +2
        (new("🐊 Cá Sấu Con",        "🐊",  200, "Uncommon"), 29), // +1
        (new("🐟 Cá Lóc Vàng",       "🐟",  300, "Rare"),     22), // 18→22
        (new("🦢 Hải Mã Bạc",        "🦢",  900, "Legendary"), 4), // 8→1→4 (tăng lên 4 để khả thi hơn ~4%)
        (new("🐢 Rùa Biển Cổ Đại",   "🐢", 3000, "Mythic"),    1), // giữ nguyên
    ];

    private static readonly (SpecialFishCatch Fish, int Weight)[] AbyssalZoneTable =
    [
        (new("🎑 Cá Đèn Lồng",              "🎑",  400, "Rare"),      57), // 35→57
        (new("🦑 Mực Vực Thẳm",             "🦑", 2000, "Legendary"), 18), // 1000→2000
        (new("🐙 Bạch Tuột Vực Sâu",         "🐙", 3000, "Legendary"), 12), // 1500→3000
        (new("🦈 Cá Mập Khổng Lồ Megalodon", "🦈", 5000, "Mythic"),    8), // 14→8
        (new("💀 Bóng Ma Biển",              "💀", 10000, "Mythic"),    5), // 1→5 (thưởng rare hơn nhưng weight thấp)
    ];

    // ── Weak-rod trash ────────────────────────────────────────────────────────
    public static readonly SpecialFishCatch WeakRodTrash =
        new("🗑️ Rác Biển", "🗑️", 0, "Trash");

    // ── Pearl chance ──────────────────────────────────────────────────────────
    private const double SeaEyeChance = 0.000001; // 0.0001%

    /// <summary>
    /// Roll 1 lần câu trong special pool.
    /// Thứ tự: WeakRod trash → Miss → Pearl → Fish roll → Escape.
    ///
    /// Miss  (IsMiss=true)  : cá không cắn — caller KHÔNG tiêu pool slot.
    /// Escape (IsEscape=true): cá cắn nhưng thoát — caller TIÊU pool slot, không có xu/XP.
    /// Trash (Rarity="Trash"): weak-rod penalty — caller TIÊU pool slot.
    /// </summary>
    public static SpecialFishCatch Roll(SpecialDropTable table, double weakRodTrashRate = 0.0)
    {
        // 1. Weak-rod penalty
        if (weakRodTrashRate > 0 && Random.Shared.NextDouble() < weakRodTrashRate)
            return WeakRodTrash;

        // 2. Miss check — cá không cắn (không tiêu slot)
        var (missRate, escapeRate) = GetPoolRates(table);
        if (Random.Shared.NextDouble() < missRate)
            return Miss;

        // 3. Pearl check
        if (Random.Shared.NextDouble() < SeaEyeChance)
            return new SpecialFishCatch("Con Mắt Biển Cả", "👁️", 25_000, "Mythic", IsPearl: true);

        // 4. Fish roll
        var entries = table switch
        {
            SpecialDropTable.CoralBay       => CoralBayTable,
            SpecialDropTable.DeepOcean      => DeepOceanTable,
            SpecialDropTable.MangroveForest => MangroveForestTable,
            SpecialDropTable.AbyssalZone    => AbyssalZoneTable,
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };

        var totalWeight = entries.Sum(e => e.Weight);
        var roll        = Random.Shared.Next(totalWeight);
        var cumulative  = 0;
        SpecialFishCatch? caught = null;

        foreach (var (fish, weight) in entries)
        {
            cumulative += weight;
            if (roll < cumulative) { caught = fish; break; }
        }

        if (caught is null)
            throw new InvalidOperationException($"SpecialFishingDropTable.Roll fell through for {table}");

        // 5. Escape check — cá cắn rồi thoát (tiêu slot, không có xu)
        if (Random.Shared.NextDouble() < escapeRate)
            return Escape with { Name = caught.Name, Emoji = caught.Emoji };

        return caught;
    }
}
