// src/Dynamite.Modules.Economy/Helpers/FishingDropTable.cs
namespace Dynamite.Modules.Economy.Helpers;

public record FishCatch(string Name, string Emoji, long Coins, string Rarity, bool IsChest = false);

/// <summary>
/// Drop table cho câu cá v2:
/// - Miss/Escape mechanic: rod tốt giảm cả hai tỉ lệ
/// - Hard cap 40%: Rare + Legendary + Mythic ≤ 40% tổng weight sau modifier
/// - Chest check riêng TRƯỚC fish roll
/// </summary>
public static class FishingDropTable
{
    // ── Tỉ lệ mặc định khi không có rod ─────────────────────────────────────
    public const double DefaultMissRate   = 0.15; // 15% — quăng cần không cá cắn
    public const double DefaultEscapeRate = 0.10; // 10% — cá cắn rồi thoát

    // ── Hard cap: Rare+Legendary+Mythic ≤ 40% tổng weight ───────────────────
    private const double RareCapFraction = 0.40;

    // ── Base weights (tổng = 108, còn chỗ cho modifier) ─────────────────────
    // Index: 0=Common, 1=Uncommon, 2=Rare, 3=Legendary, 4=Mythic (KHÔNG thay đổi)
    // Index: 5-8=Trash (thêm vào cuối, không ảnh hưởng rare cap logic)
    private static readonly (int Weight, FishCatch Template)[] BaseTable =
    [
        (50, new("Cá Thường",       "🐟", 0, "Common")),
        (28, new("Cá Hiếm Vừa",    "🐠", 0, "Uncommon")),
        (14, new("Cá Hiếm",         "🐡", 0, "Rare")),
        (5,  new("Cá Huyền Thoại",  "🦈", 0, "Legendary")),
        (1,  new("Cá Thần",         "🐉", 0, "Mythic")),
        // ── Trash tier (tổng weight 10 ≈ 9.3%) ────────────────────────────────
        (4,  new("Rác",             "🗑️", 0, "Trash")),
        (3,  new("Vỏ Lon",          "🥫", 0, "Trash")),
        (2,  new("Bao Rác",         "🛍️", 0, "Trash")),
        (1,  new("Quần Cũ",         "👖", 0, "Trash")),
    ];

    // Chest check theo thứ tự Diamond → Gold → Bronze
    private static readonly (string Name, string Emoji, string Rarity, double BaseChance)[] Chests =
    [
        ("Hòm Kim Cương", "💎", "Diamond", 0.002),
        ("Hòm Vàng",      "🪙", "Gold",    0.010),
        ("Hòm Đồng",      "📦", "Bronze",  0.030),
    ];

    private static readonly Dictionary<string, (long Min, long Max)> CoinRanges = new()
    {
        ["Common"]    = (10,   40),
        ["Uncommon"]  = (50,   100),
        ["Rare"]      = (120,  250),
        ["Legendary"] = (350,  600),
        ["Mythic"]    = (700,  1200),
        ["Bronze"]    = (200,  500),
        ["Gold"]      = (600,  1500),
        ["Diamond"]   = (2000, 5000),
        ["Trash"]     = (0,    0),   // rác — 0 coins
    };

    /// <summary>
    /// Trả về kết quả câu sau khi đã qua miss/escape check.
    /// Caller phải truyền miss/escape rate của rod hiện tại.
    /// </summary>
    public static RollResult Roll(
        double missRate       = DefaultMissRate,
        double escapeRate     = DefaultEscapeRate,
        double dropMultiplier = 1.0,
        double rareMod        = 0.0,
        double legendaryMod   = 0.0,
        double missMod        = 0.0)   // weather miss modifier (âm = ít miss, dương = nhiều miss)
    {
        // ── 1. Miss check (TRƯỚC pond consumption — không tốn cá pond) ───────
        var effectiveMiss = Math.Clamp(missRate + missMod, 0.0, 0.60); // cap tổng miss [0%, 60%]
        if (Random.Shared.NextDouble() < effectiveMiss)
            return new RollResult(RollOutcome.Miss, null);

        // ── 2. Chest check (Diamond → Gold → Bronze) ─────────────────────────
        foreach (var (name, emoji, rarity, baseChance) in Chests)
        {
            if (Random.Shared.NextDouble() < baseChance * dropMultiplier)
            {
                var (cMin, cMax) = CoinRanges[rarity];
                var chest = new FishCatch(name, emoji,
                    Random.Shared.NextInt64(cMin, cMax + 1), rarity, IsChest: true);

                // Chest cũng có thể bị escape (nhưng tỉ lệ thấp hơn 50%)
                if (Random.Shared.NextDouble() < escapeRate * 0.5)
                    return new RollResult(RollOutcome.Escape, null);

                return new RollResult(RollOutcome.Caught, chest);
            }
        }

        // ── 3. Fish roll với 40% rare cap ────────────────────────────────────
        var table = BuildModifiedTableWithCap(rareMod, legendaryMod);
        var totalWeight = table.Sum(t => t.Weight);
        var roll = Random.Shared.Next(totalWeight);
        var cumulative = 0;
        FishCatch? rolledFish = null;

        foreach (var (weight, template) in table)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                var (min, max) = CoinRanges[template.Rarity];
                var coins = (long)(Random.Shared.NextInt64(min, max + 1) * dropMultiplier);
                rolledFish = template with { Coins = coins };
                break;
            }
        }

        if (rolledFish is null)
            throw new InvalidOperationException("FishingDropTable: no fish selected (weight mismatch)");

        // ── 4. Escape check (SAU khi đã roll — cá thoát) ────────────────────
        // Cá hiếm hơn → khó nắm hơn → escape rate cao hơn theo tier
        var tierEscapeMultiplier = rolledFish.Rarity switch
        {
            "Legendary" => 1.5,
            "Mythic"    => 2.0,
            _           => 1.0
        };
        var effectiveEscape = Math.Min(escapeRate * tierEscapeMultiplier, 0.50); // cap 50%

        if (Random.Shared.NextDouble() < effectiveEscape)
            return new RollResult(RollOutcome.Escape, rolledFish); // biết cá gì nhưng hụt

        return new RollResult(RollOutcome.Caught, rolledFish);
    }

    // ── 40% cap modifier ─────────────────────────────────────────────────────

    private static List<(int Weight, FishCatch Template)> BuildModifiedTableWithCap(
        double rareMod, double legendaryMod)
    {
        var table = BaseTable.Select(t => (t.Weight, t.Template)).ToList();

        var rareBonus      = (int)(table[2].Weight * rareMod);
        var legendaryBonus = (int)(table[3].Weight * legendaryMod);
        var totalBonus     = rareBonus + legendaryBonus;

        // Trừ từ Common trước, rồi Uncommon
        var fromCommon   = Math.Min(table[0].Weight - 1, totalBonus);
        var fromUncommon = Math.Min(table[1].Weight - 1, totalBonus - fromCommon);

        table[0] = (table[0].Weight - fromCommon,    table[0].Template);
        table[1] = (table[1].Weight - fromUncommon,  table[1].Template);
        table[2] = (table[2].Weight + rareBonus,     table[2].Template);
        table[3] = (table[3].Weight + legendaryBonus, table[3].Template);

        // ── Hard cap: Rare + Legendary + Mythic ≤ 40% ───────────────────────
        var total         = table.Sum(t => t.Weight);
        var rareGroupSum  = table[2].Weight + table[3].Weight + table[4].Weight;
        var maxRareWeight = (int)(total * RareCapFraction);

        if (rareGroupSum > maxRareWeight)
        {
            // Scale down proportionally, tối thiểu 1 weight mỗi tier
            var scale = (double)maxRareWeight / rareGroupSum;
            var newRare      = Math.Max(1, (int)(table[2].Weight * scale));
            var newLegendary = Math.Max(1, (int)(table[3].Weight * scale));
            var newMythic    = Math.Max(1, (int)(table[4].Weight * scale));

            var overflow = (newRare + newLegendary + newMythic) - maxRareWeight;
            // Đổ phần overflow về Common
            table[0] = (table[0].Weight + overflow, table[0].Template);
            table[2] = (newRare,      table[2].Template);
            table[3] = (newLegendary, table[3].Template);
            table[4] = (newMythic,    table[4].Template);
        }

        return table;
    }

    public static (long Min, long Max) GetCoinRange(string rarity)
        => CoinRanges.GetValueOrDefault(rarity, (10, 40));
}

// ── Result types ──────────────────────────────────────────────────────────────

public enum RollOutcome { Caught, Miss, Escape }

/// <summary>
/// Kết quả một lần roll.
/// - Caught: bắt được cá (Fish != null)
/// - Miss:   không cắn câu (Fish == null, không tốn fish pool)
/// - Escape: cá cắn rồi thoát (Fish = loại cá đã roll, tốn fish pool)
/// </summary>
public record RollResult(RollOutcome Outcome, FishCatch? Fish);
