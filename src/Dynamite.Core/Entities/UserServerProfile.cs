// src/Dynamite.Core/Entities/UserServerProfile.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Server-level XP pool riêng (chat + voice).
/// Tách hoàn toàn với FishingXP.
/// </summary>
public class UserServerProfile : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // ── XP + Level ──────────────────────────────────────────────────────────
    public long ServerXp { get; set; } = 0;
    public int ServerLevel { get; set; } = 0;

    // ── Anti-spam: cooldown tin nhắn ─────────────────────────────────────────
    /// <summary>Thời điểm lần cuối nhận XP từ tin nhắn (1 phút cooldown).</summary>
    public DateTime? LastMessageXpAt { get; set; }

    // ── Voice tracking ───────────────────────────────────────────────────────
    /// <summary>
    /// Thời điểm user join voice. Null = không đang trong voice.
    /// Mỗi 5 phút tích lũy, XP được cộng và field này được cập nhật.
    /// </summary>
    public DateTime? VoiceJoinedAt { get; set; }

    /// <summary>Tổng số phút đã ngồi voice (dùng cho stats/achievements).</summary>
    public int TotalVoiceMinutes { get; set; } = 0;
}
