// src/Dynamite.Core/Entities/GuildPresence.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Track xem bot hiện có đang trong guild này không.
/// 
/// Tại sao standalone, không FK vào GuildConfig?
/// → Bot có thể join một guild mà chưa có GuildConfig (user chưa setup).
/// → GuildPresence phải tồn tại độc lập để BotPresent check hoạt động
///   ngay cả với guilds "mới tinh".
/// → Tránh circular dependency khi sync on startup.
/// </summary>
public class GuildPresence : BaseEntity
{
    public ulong GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public string? IconHash { get; set; }
    public bool IsPresent { get; set; } = true;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}