// src/Dynamite.Modules/Setup/Templates/SetupTemplate.cs
namespace Dynamite.Modules.Setup.Templates;

using Discord;

// ─── Data models ──────────────────────────────────────────────────────────────
// Những class này là pure data — không có logic, không có Discord API calls.
// SetupExecutor đọc template này và thực hiện các Discord API calls.
//
// Design decisions:
//   - Additive only: bot chỉ TẠO, không xóa thứ gì đã tồn tại
//   - Template là static config, không lưu DB — không cần persist
//   - Permission overwrites được định nghĩa rõ ràng per channel
// ──────────────────────────────────────────────────────────────────────────────

public class SetupTemplate
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<RoleTemplate> Roles { get; init; } = [];
    public IReadOnlyList<CategoryTemplate> Categories { get; init; } = [];
}

public class RoleTemplate
{
    public string Name { get; init; } = string.Empty;
    public Color Color { get; init; } = Color.Default;
    public bool Mentionable { get; init; } = false;
    public bool Hoisted { get; init; } = false; // hiện riêng trong member list

    // Permissions cụ thể role này sẽ có. null = không set (dùng default guild perms)
    public GuildPermission? Permissions { get; init; }
}

public class CategoryTemplate
{
    public string Name { get; init; } = string.Empty;

    // Danh sách channels trong category này
    public IReadOnlyList<ChannelTemplate> Channels { get; init; } = [];

    // Permission overwrites cho cả category (inherit xuống channels)
    public IReadOnlyList<PermissionOverwriteTemplate> Overwrites { get; init; } = [];
}

public class ChannelTemplate
{
    public string Name { get; init; } = string.Empty;
    public ChannelType Type { get; init; } = ChannelType.Text;
    public string? Topic { get; init; }

    // Slow mode tính bằng giây. 0 = tắt.
    public int SlowModeInterval { get; init; } = 0;

    // Override permissions riêng cho channel này (ngoài category overwrites)
    public IReadOnlyList<PermissionOverwriteTemplate> Overwrites { get; init; } = [];
}

// Định nghĩa overwrite: target có thể là "@everyone" hoặc tên role từ Roles list
// Bot sẽ resolve tên → role object sau khi tạo role
public class PermissionOverwriteTemplate
{
    // "@everyone" = guild everyone role; tên khác = tên role trong Roles list
    public string TargetRoleName { get; init; } = "@everyone";

    public OverwritePermissions Permissions { get; init; } = OverwritePermissions.InheritAll;
}