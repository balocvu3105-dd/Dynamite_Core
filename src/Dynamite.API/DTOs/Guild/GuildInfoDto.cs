// src/Dynamite.API/DTOs/Guild/GuildInfoDto.cs
namespace Dynamite.API.DTOs.Guild;

/// <summary>
/// Full guild info cho dashboard — channels + roles.
/// Dashboard dùng để render dropdowns (chọn log channel, chọn mod role, v.v.)
/// </summary>
public record GuildInfoDto(
    string Id,
    string Name,
    string? IconUrl,
    bool BotPresent,
    IEnumerable<ChannelDto> Channels,
    IEnumerable<RoleDto> Roles
);

public record ChannelDto(
    string Id,
    string Name,
    string Type   // "text", "voice", "category", "announcement"
);

public record RoleDto(
    string Id,
    string Name,
    string Color,  // hex string, ví dụ "#FF5733"
    bool IsManaged // true = bot role, không cho user tự assign
);