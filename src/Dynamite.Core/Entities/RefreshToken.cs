// src/Dynamite.Core/Entities/RefreshToken.cs
namespace Dynamite.Core.Entities;

public class RefreshToken : BaseEntity
{
    /// <summary>
    /// Discord user ID dạng string.
    /// Không dùng ulong ở đây vì RefreshToken không liên kết với GuildConfig.
    /// </summary>
    public string DiscordUserId { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; } = false;

    // Computed — không map vào DB column
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}