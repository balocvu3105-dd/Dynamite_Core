// src/Dynamite.Bot/Settings/ScheduledRestartSettings.cs
namespace Dynamite.Bot.Settings;

public sealed class ScheduledRestartSettings
{
    /// <summary>
    /// Enable/disable scheduled restart.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Giờ restart (0–23), tính theo TimeZoneId.
    /// Default: 3 (3:00 AM)
    /// </summary>
    public int Hour { get; set; } = 3;

    /// <summary>
    /// Phút restart (0–59).
    /// Default: 0
    /// </summary>
    public int Minute { get; set; } = 0;

    /// <summary>
    /// IANA timezone ID hoặc Windows timezone ID.
    /// Default: "SE Asia Standard Time" (ICT, UTC+7)
    /// Alternatives: "Asia/Ho_Chi_Minh" trên Linux
    /// </summary>
    public string TimeZoneId { get; set; } = "SE Asia Standard Time";
}