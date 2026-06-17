// src/Dynamite.Core/Entities/LeaderboardSnapshot.cs
namespace Dynamite.Core.Entities;

public enum LeaderboardType
{
    Fishing, // Tổng cá câu trong tuần
    Chat,    // Tin nhắn gửi trong tuần
    Voice    // Phút ngồi voice trong tuần
}

/// <summary>
/// Snapshot bảng xếp hạng hàng tuần — chụp mỗi CN 12h trưa.
/// Không query real-time — hiển thị từ snapshot này.
/// </summary>
public class LeaderboardSnapshot : BaseEntity
{
    public ulong GuildId { get; set; }
    public LeaderboardType Type { get; set; }
    public DateTime WeekStartDate { get; set; } // Thứ Hai của tuần đó

    public ICollection<LeaderboardEntry> Entries { get; set; } = [];
}

public class LeaderboardEntry : BaseEntity
{
    public Guid SnapshotId { get; set; }
    public ulong GuildId   { get; set; }
    public ulong UserId    { get; set; }
    public int   Rank      { get; set; }
    public long  Value     { get; set; } // Số cá / số tin / số phút
    public int   DeltaRank { get; set; } // Thay đổi rank so với tuần trước (+/-/0)

    public LeaderboardSnapshot Snapshot { get; set; } = null!;
}

/// <summary>
/// Đếm hoạt động tuần này (reset mỗi CN 12h sau khi snapshot xong).
/// Tách khỏi UserServerProfile để dễ reset mà không ảnh hưởng total XP.
/// </summary>
public class WeeklyActivity : BaseEntity
{
    public ulong GuildId          { get; set; }
    public ulong UserId           { get; set; }
    public int   WeeklyFishCaught { get; set; } = 0;
    public int   WeeklyMessages   { get; set; } = 0;
    public int   WeeklyVoiceMinutes { get; set; } = 0;
    public DateTime WeekResetAt   { get; set; } // Khi nào reset tuần tiếp theo
}
