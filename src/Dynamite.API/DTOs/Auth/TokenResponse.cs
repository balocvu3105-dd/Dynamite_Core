// src/Dynamite.API/DTOs/Auth/TokenResponse.cs
namespace Dynamite.API.DTOs.Auth;

/// <summary>
/// Response trả về sau khi login thành công.
/// AccessToken dùng cho mọi API request.
/// RefreshToken dùng để lấy AccessToken mới khi hết hạn.
/// </summary>
public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DiscordUserDto User
);

/// <summary>
/// Request body cho /auth/refresh
/// </summary>
public record RefreshRequest(string RefreshToken);