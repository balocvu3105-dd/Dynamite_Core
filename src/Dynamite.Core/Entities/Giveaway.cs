// src/Dynamite.Core/Entities/Giveaway.cs
namespace Dynamite.Core.Entities;
public class Giveaway : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong HostId { get; set; }
    public string Prize { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int WinnerCount { get; set; } = 1;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsEnded { get; set; } = false;
    public bool IsCancelled { get; set; } = false;
    // Comma-separated winner user IDs (sau khi end)
    public string? WinnerIds { get; set; }

    // Role được tag khi đăng giveaway — chỉ ai có role này được thông báo. Null = không tag.
    public ulong? PingRoleId { get; set; }

    // Điều kiện tham gia: số ngày tối thiểu phải ở trong server. 0 = không yêu cầu.
    public int MinJoinDays { get; set; } = 0;

    // Điều kiện tham gia: phải join server TRƯỚC mốc ngày cố định này (UTC). Null = không yêu cầu.
    public DateTime? JoinedBefore { get; set; }

    // Nội dung DM gửi người trúng để hướng dẫn nhận thưởng. Null = dùng mẫu mặc định.
    public string? ClaimMessage { get; set; }
    // Pre-selection — Server Owner có thể chỉ định nhiều winner trước khi hết giờ
    // Comma-separated user IDs, max = WinnerCount. Nếu được set, timer dùng list này thay vì random.
    public string? PreSelectedWinnerIds { get; set; }
    public DateTime? PreSelectedAt { get; set; }
    public ulong? PreSelectedBy { get; set; }

    // Helper — parse PreSelectedWinnerIds thành List<ulong>
    public List<ulong> GetPreSelectedWinners()
        => string.IsNullOrWhiteSpace(PreSelectedWinnerIds)
            ? []
            : PreSelectedWinnerIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => ulong.TryParse(s, out var id) ? id : 0UL)
                .Where(id => id != 0)
                .ToList();
    public ICollection<GiveawayEntry> Entries { get; set; } = [];
}