// src/Dynamite.Core/Entities/FishingActivityLog.cs
namespace Dynamite.Core.Entities;

public enum FishingEvent
{
    Caught,     // câu thành công
    Miss,       // quăng cần nhưng không cá cắn
    Escape,     // cá cắn rồi thoát
    BagFull,    // câu được nhưng túi đầy, chỉ nhận coins
    PearlCaught,   // câu được ngọc quý (subset của Caught)
    PearlCapHit,   // pearl rolled nhưng cap đã đầy
    StormBreak,    // đứt cước do bão
    SpecialCaught, // câu ở special pool thành công
    SpecialEscape, // thoát khỏi special pool
}

/// <summary>
/// Log toàn bộ hoạt động câu cá — dùng cho audit, restore, debug.
/// Không xóa; rotate theo thời gian nếu cần (giữ 90 ngày mặc định).
/// </summary>
public class FishingActivityLog : BaseEntity
{
    public ulong  GuildId    { get; set; }
    public ulong  UserId     { get; set; }

    public FishingEvent Event { get; set; }

    /// <summary>Tên cá hoặc item được roll (null nếu Miss).</summary>
    public string? FishName { get; set; }
    public string? Rarity   { get; set; }

    /// <summary>Coins nhận được (0 nếu Miss/Escape).</summary>
    public long CoinsEarned { get; set; }

    /// <summary>Fishing XP nhận được.</summary>
    public int XpEarned { get; set; }

    /// <summary>Tên pool (null = main pond, "Vịnh San Hô" = special pool).</summary>
    public string? PoolName { get; set; }

    /// <summary>Cần câu đang dùng (null = không có).</summary>
    public string? RodName { get; set; }

    /// <summary>Thời tiết khi câu.</summary>
    public string? Weather { get; set; }

    /// <summary>Số cá còn lại trong pond sau lần này.</summary>
    public int PondRemaining { get; set; }
}
