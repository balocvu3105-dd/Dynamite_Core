// src/Dynamite.Core/Entities/FishingDataSnapshot.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Snapshot trạng thái fishing của 1 user tại một thời điểm.
/// Dùng để restore khi hệ thống gặp sự cố.
///
/// Giữ tối đa 5 snapshot gần nhất per user (cái cũ nhất bị xóa tự động).
/// Trigger: level up, achievement, Mythic/Pearl catch, weekly schedule, manual admin.
/// </summary>
public class FishingDataSnapshot : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId  { get; set; }

    /// <summary>Lý do chụp snapshot (auto-levelup / auto-weekly / auto-milestone / manual).</summary>
    public string Reason { get; set; } = "manual";

    // ── Fishing Profile state ─────────────────────────────────────────────────
    public long FishingXp       { get; set; }
    public int  FishingLevel    { get; set; }
    public int  TotalCaught     { get; set; }
    public int  CommonCaught    { get; set; }
    public int  UncommonCaught  { get; set; }
    public int  RareCaught      { get; set; }
    public int  LegendaryCaught { get; set; }
    public int  MythicCaught    { get; set; }
    public int  ChestsOpened    { get; set; }

    // ── Wallet state ──────────────────────────────────────────────────────────
    public long WalletCoins { get; set; }

    // ── Bag state (serialized JSON) ───────────────────────────────────────────
    /// <summary>JSON array của CaughtFish snapshot (lightweight, không FK).</summary>
    public string BagSnapshotJson { get; set; } = "[]";
    public int    BagCapacity     { get; set; }

    // ── Achievements (comma-separated IDs) ───────────────────────────────────
    public string AchievementIds { get; set; } = string.Empty;
}
