// src/Dynamite.API/DTOs/Auth/DiscordUserDto.cs
namespace Dynamite.API.DTOs.Auth;

/// <summary>
/// Data trả về từ Discord API /users/@me
/// </summary>
public record DiscordUserDto(
    string Id,
    string Username,
    string? Avatar,
    string? Email
);

/// <summary>
/// Guild item trả về từ Discord API /users/@me/guilds
/// </summary>
public record DiscordGuildDto(
    string Id,
    string Name,
    string? Icon,
    long Permissions  // bitmask — dùng để check ManageGuild
);