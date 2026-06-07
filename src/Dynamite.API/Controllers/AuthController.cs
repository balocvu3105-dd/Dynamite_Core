// src/Dynamite.API/Controllers/AuthController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.Auth;
using Dynamite.API.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly DiscordOAuthService _discord;
    private readonly JwtService _jwt;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        DiscordOAuthService discord,
        JwtService jwt,
        ILogger<AuthController> logger)
    {
        _discord = discord;
        _jwt = jwt;
        _logger = logger;
    }

    /// <summary>
    /// GET /auth/login
    /// Redirect user sang Discord OAuth2 authorization page.
    /// </summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        var url = _discord.BuildOAuthUrl();
        return Redirect(url);
    }

    /// <summary>
    /// GET /auth/callback?code=xxx
    /// Discord redirect về đây sau khi user authorize.
    /// Exchange code lấy Discord token → issue JWT.
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        // User từ chối authorize
        if (error is not null || code is null)
            return BadRequest(new { error = "Authorization was denied or code is missing." });

        // Exchange code → Discord access token
        var discordAccessToken = await _discord.ExchangeCodeAsync(code, ct);

        // Lấy user info từ Discord
        var discordUser = await _discord.GetCurrentUserAsync(discordAccessToken, ct);

        // Issue JWT
        var (accessToken, expiry) = _jwt.GenerateAccessToken(
            discordUser.Id,
            discordUser.Username,
            discordUser.Avatar);

        var refreshToken = await _jwt.GenerateRefreshTokenAsync(discordUser.Id, ct);

        // Set cả cookie lẫn trả về body — support 2 cách
        SetRefreshTokenCookie(refreshToken);

        _logger.LogInformation("User {UserId} ({Username}) logged in",
            discordUser.Id, discordUser.Username);

        return Ok(new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiry: expiry,
            User: discordUser));
    }

    /// <summary>
    /// POST /auth/refresh
    /// Dùng refresh token để lấy access token mới.
    /// Đọc từ cookie hoặc request body.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest? body,
        CancellationToken ct)
    {
        // Ưu tiên cookie, fallback về body
        var token = Request.Cookies["refresh_token"]
            ?? body?.RefreshToken;

        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { error = "Refresh token is missing." });

        var discordUserId = await _jwt.ValidateRefreshTokenAsync(token, ct);
        if (discordUserId is null)
            return Unauthorized(new { error = "Refresh token is invalid or expired." });

        // Revoke token cũ và issue cặp token mới
        await _jwt.RevokeRefreshTokenAsync(token, ct);

        var (accessToken, expiry) = _jwt.GenerateAccessToken(
            discordUserId, string.Empty, null);

        var newRefreshToken = await _jwt.GenerateRefreshTokenAsync(discordUserId, ct);

        SetRefreshTokenCookie(newRefreshToken);

        return Ok(new
        {
            accessToken,
            refreshToken = newRefreshToken,
            accessTokenExpiry = expiry
        });
    }

    /// <summary>
    /// POST /auth/logout
    /// Revoke refresh token → user phải login lại sau 15 phút.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest? body,
        CancellationToken ct)
    {
        var token = Request.Cookies["refresh_token"]
            ?? body?.RefreshToken;

        if (!string.IsNullOrEmpty(token))
            await _jwt.RevokeRefreshTokenAsync(token, ct);

        // Xóa cookie
        Response.Cookies.Delete("refresh_token");
        Response.Cookies.Delete("access_token");

        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// GET /auth/me
    /// Trả về thông tin user hiện tại từ JWT claims.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        var username = User.FindFirst("username")?.Value;
        var avatar = User.FindFirst("avatar")?.Value;

        return Ok(new { userId, username, avatar });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,   // JS không đọc được — chống XSS
            Secure = true,   // Chỉ gửi qua HTTPS
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}