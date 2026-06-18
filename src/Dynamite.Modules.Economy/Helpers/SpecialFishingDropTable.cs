// src/Dynamite.Modules.Economy/Helpers/SpecialFishingDropTable.cs
namespace Dynamite.Modules.Economy.Helpers;

using Dynamite.Core.Entities;

/// <summary>
/// Drop table cho Special Pool (level 20+).
/// Mỗi SpecialDropTable enum có bảng giống loài riêng.
/// Pearl "Con Mắt Biển Cả" được check cap ở SpecialPoolService,
/// không nằm trong weight table — được roll riêng với xác suất 0.00001%.
/// </summary>
public static class SpecialFishingDropTable
{
    public record SpecialFishCatch(
        string Name,
        string Emoji,
        long   Coins,
        string Rarity,
        bool   IsPearl = false);

    // ── Creatures per pool ────────────────────────────────────────────────────

    private static readonly (SpecialFishCatch Fish, int Weight)[] CoralBayTable =
    [
        (new("🦞 Tôm Hùm",            "🦞", 150,  "Common"),    40),
        (new("🦑 Mực Khổng Lồ",        "🦑", 280,  "Uncommon"),  30),
        (new("🐙 Bạch Tuột",            "🐙", 350,  "Uncommon"),  18),
        (new("🪼 Sứa Phát Quang",       "🪼", 700,  "Rare"),       9),
        (new("🦈 Cá Mập Trắng",         "🦈", 2500, "Legendary"),  3),
    ];

    private static readonly (SpecialFishCatch Fish, int Weight)[] DeepOceanTable =
    [
        (new("🐙 Bạch Tuột Khổng Lồ",   "🐙", 500,  "Uncommon"),  35),
        (new("🦑 Mực Ma Quái",           "🦑", 600,  "Uncommon"),  25),
        (new("🪼 Sứa Xanh Điện",         "🪼", 1200, "Rare"),      20),
        (new("🦈 Cá Mập Trắng Lớn",      "🦈", 4000, "Legendary"), 15),
        (new("🐋 Cá Voi Xanh",           "🐋", 8000, "Mythic"),     5),
    ];

    private static readonly (SpecialFishCatch Fish, int Weight)[] MangroveForestTable =
    [
        (new("🦐 Tôm Nước Ngọt",          "🦐", 100,  "Common"),   45),
        (new("🐊 Cá Sấu Con",              "🐊", 400,  "Uncommon"), 28),
        (new("🐟 Cá Lóc Vàng",             "🐟", 600,  "Rare"),     18),
        (new("🦢 Hải Mã Bạc",              "🦢", 1800, "Legendary"), 8),
        (new("🌿 Rùa Biển Cổ Đại",         "🐢", 6000, "Mythic"),    1),
    ];

    private static readonly (SpecialFishCatch Fish, int Weight)[] AbyssalZoneTable =
    [
        (new("🌑 Cá Đèn Lồng",             "🎑", 800,  "Rare"),      35),
        (new("🦑 Mực Vực Thẳm",            "🦑", 2000, "Legendary"), 30),
        (new("🐙 Bạch Tuột Vực Sâu",        "🐙", 3000, "Legendary"), 20),
        (new("🦈 Cá Mập Khổng Lồ Megalodon","🦈", 10000,"Mythic"),   14),
        (new("💀 Bóng Ma Biển",             "💀", 20000,"Mythic"),    1),
    ];

    // ── Weak-rod trash (penalty when using Cần Câu Tre/Bạc in special pool) ────

    /// <summary>
    /// Cá rác vớt được khi cần câu quá yếu cho pool đặc biệt.
    /// Chiếm 1 slot trong pool (fishing attempt consumed) nhưng 0 giá trị.
    /// </summary>
    public static readonly SpecialFishCatch WeakRodTrash =
        new("🗑️ Rác Biển", "🗑️", 0, "Trash");

    // ── Pearl chance (rolled independently, check cap before awarding) ───────

    private const double SeaEyeChance = 0.000001; // 0.0001%

    /// <summary>
    /// Roll 1 lần câu trong special pool.
    /// Pearl check phải được xử lý bởi caller (cap + log).
    /// <paramref name="weakRodTrashRate"/> — tỉ lệ ra rác khi dùng cần câu yếu (Cần Tre: 0.20, Cần Bạc: 0.10).
    /// </summary>
    public static SpecialFishCatch Roll(SpecialDropTable table, double weakRodTrashRate = 0.0)
    {
        // Weak-rod penalty: cần yếu → tỉ lệ ra rác cao khi câu pool đặc biệt
        if (weakRodTrashRate > 0 && Random.Shared.NextDouble() < weakRodTrashRate)
            return WeakRodTrash;

        // Pearl check first (extremely rare)
        if (Random.Shared.NextDouble() < SeaEyeChance)
            return new SpecialFishCatch("Con Mắt Biển Cả", "👁️", 50_000, "Mythic", IsPearl: true);

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

        foreach (var (fish, weight) in entries)
        {
            cumulative += weight;
            if (roll < cumulative) return fish;
        }

        // Unreachable — safety net
        throw new InvalidOperationException($"SpecialFishingDropTable.Roll fell through for {table}");
    }
}
