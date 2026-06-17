// src/Dynamite.Core/Entities/UserFishingAchievement.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Tracking thành tựu đã đạt — để không award trùng.
/// AchievementId là string constant (xem AchievementIds).
/// </summary>
public class UserFishingAchievement : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string AchievementId { get; set; } = string.Empty;
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserFishingProfile Profile { get; set; } = null!;
}

/// <summary>Danh sách ID thành tựu — dùng constant để tránh magic string.</summary>
public static class AchievementIds
{
    public const string FirstCatch        = "first_catch";
    public const string FirstChest        = "first_chest";
    public const string Catch50Common     = "catch_50_common";
    public const string Catch50Uncommon   = "catch_50_uncommon";
    public const string Catch50Rare       = "catch_50_rare";
    public const string Catch50Legendary  = "catch_50_legendary";
    public const string Catch10Mythic     = "catch_10_mythic";
    public const string Catch500Total     = "catch_500_total";
    public const string Catch1000Total    = "catch_1000_total";
    public const string OpenGoldChest     = "open_gold_chest";
    public const string OpenDiamondChest  = "open_diamond_chest";
    public const string FishingLevel10    = "fishing_level_10";
    public const string FishingLevel50    = "fishing_level_50";
    public const string FishingLevel100   = "fishing_level_100";
}
