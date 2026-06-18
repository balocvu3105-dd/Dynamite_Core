// src/Dynamite.Core/Common/Results/EconomyResults.cs
namespace Dynamite.Core.Common.Results;

using Dynamite.Core.Entities;

/// <summary>Kết quả mua item từ shop.</summary>
public record BuyResult(
    InventoryItem Item,
    long          CoinsPaid,
    long          CoinsRemaining,
    string        DisplayMessage);

/// <summary>Kết quả sửa cần câu.</summary>
public record RepairRodResult(
    InventoryItem Item,
    int           OldDurability,
    int           NewDurability,
    long          CoinsPaid,
    long          CoinsRemaining);

/// <summary>Kết quả dùng item tiêu thụ (bait, weather, pool ticket...).</summary>
public record UseItemResult(
    InventoryItem Item,
    string        EffectDescription);

/// <summary>Kết quả nhận daily reward.</summary>
public record DailyResult(
    long CoinsEarned,
    long TotalCoins,
    int  Streak);

/// <summary>Kết quả transfer coins.</summary>
public record TransferResult(
    long FromCoinsRemaining,
    long ToCoinsNew);

/// <summary>Kết quả nâng túi cá.</summary>
public record BagUpgradeResult(
    int  OldCapacity,
    int  NewCapacity,
    long CoinsPaid);
