// src/Dynamite.Core/Entities/GuildPond.cs
namespace Dynamite.Core.Entities;

public enum PondWeather
{
    Sunny,   // base rates
    Rainy,   // Rare +15%, Legendary +5%
    Stormy   // Legendary +5%, nhưng 20% chance "đứt cước" (mất lượt, không trừ cá)
}

/// <summary>
/// Trạng thái bể cá của một guild. Mỗi guild có đúng 1 bản ghi.
/// Capacity mặc định 5000 — hết → lock 30 phút rồi reset.
/// </summary>
public class GuildPond : BaseEntity
{
    public ulong GuildId { get; set; }

    public int CurrentFish { get; set; } = 5000;
    public int MaxFish { get; set; } = 5000;

    /// <summary>Thời điểm bể bị cạn (null = chưa bao giờ hoặc đã reset).</summary>
    public DateTime? DepletedAt { get; set; }

    /// <summary>
    /// Thời điểm bể sẵn sàng được reset (DepletedAt + 30 phút).
    /// Null = bể đang có cá.
    /// </summary>
    public DateTime? ResetAvailableAt { get; set; }

    public PondWeather CurrentWeather { get; set; } = PondWeather.Sunny;
    public DateTime WeatherExpiresAt { get; set; } = DateTime.UtcNow;

    /// <summary>Channel dành riêng cho daily điểm danh. Null = chưa cấu hình.</summary>
    public ulong? DailyChannelId { get; set; }

    /// <summary>Channel dành riêng cho câu cá. Null = bất kỳ channel.</summary>
    public ulong? FishingChannelId { get; set; }

    /// <summary>
    /// Override tỉ lệ câu hụt toàn guild. Null = dùng default rod/item rate.
    /// Range [0.0, 1.0]. Ví dụ: 0.0 = không bao giờ hụt, 0.5 = 50% hụt.
    /// </summary>
    public double? FishMissRateOverride { get; set; }

    /// <summary>
    /// Override tỉ lệ cá thoát toàn guild. Null = dùng default rod/item rate.
    /// Range [0.0, 1.0].
    /// </summary>
    public double? FishEscapeRateOverride { get; set; }

    public bool IsEmpty => CurrentFish <= 0;
    public bool CanReset => ResetAvailableAt.HasValue && DateTime.UtcNow >= ResetAvailableAt.Value;
}
