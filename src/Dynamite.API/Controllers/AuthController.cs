// src/Dynamite.API/Controllers/AuthController.cs
namespace Dynamite.API.Controllers;

using System.Security.Cryptography;
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
    /// POST /api/auth/discord
    /// SPA flow: frontend gửi code lên đây, backend exchange lấy Discord token → issue JWT.
    /// </summary>
    [HttpPost("/api/auth/discord")]
    [AllowAnonymous]
    public async Task<IActionResult> SpaLogin(
        [FromBody] SpaLoginRequest body,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(body.Code))
            return BadRequest(new { error = "Code is required." });

        try
        {
            // Exchange code → Discord access token
            var discordAccessToken = await _discord.ExchangeCodeAsync(body.Code, body.RedirectUri, ct);

            // Lấy user info từ Discord
            var discordUser = await _discord.GetCurrentUserAsync(discordAccessToken, ct);

            // Issue JWT
            var (accessToken, expiry) = _jwt.GenerateAccessToken(
                discordUser.Id,
                discordUser.Username,
                discordUser.Avatar);

            var refreshToken = await _jwt.GenerateRefreshTokenAsync(
                discordUser.Id, discordUser.Username, discordUser.Avatar, ct);

            _logger.LogInformation("User {UserId} ({Username}) logged in via SPA",
                discordUser.Id, discordUser.Username);

            // Trả về accessToken + discordToken (để frontend gửi X-Discord-Token header)
            return Ok(new
            {
                accessToken,
                refreshToken,
                discordToken = discordAccessToken,
                expiresIn = (int)(expiry - DateTime.UtcNow).TotalSeconds,
                user = new
                {
                    id = discordUser.Id,
                    username = discordUser.Username,
                    avatar = discordUser.Avatar,
                    email = discordUser.Email,
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SPA login with code {Code}", body.Code);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /auth/login
    /// Redirect user sang Discord OAuth2 authorization page.
    /// Generates a random state value, stores it in an HttpOnly cookie,
    /// and embeds it in the OAuth URL for CSRF protection.
    /// </summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append("oauth_state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Lax,
            MaxAge   = TimeSpan.FromMinutes(10)
        });
        var url = _discord.BuildOAuthUrl(state);
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
        [FromQuery] string? state,
        CancellationToken ct)
    {
        if (error is not null || code is null)
            return BadRequest(new { error = "Authorization was denied or code is missing." });

        // CSRF check: state must match the value stored in the cookie during /auth/login
        var storedState = Request.Cookies["oauth_state"];
        if (string.IsNullOrEmpty(state) || storedState != state)
            return BadRequest(new { error = "Invalid state parameter. Possible CSRF attack." });

        Response.Cookies.Delete("oauth_state");

        try
        {
            var discordAccessToken = await _discord.ExchangeCodeAsync(code, null, ct);
            var discordUser = await _discord.GetCurrentUserAsync(discordAccessToken, ct);

            var (accessToken, expiry) = _jwt.GenerateAccessToken(
                discordUser.Id,
                discordUser.Username,
                discordUser.Avatar);

            var refreshToken = await _jwt.GenerateRefreshTokenAsync(
                discordUser.Id, discordUser.Username, discordUser.Avatar, ct);

            SetRefreshTokenCookie(refreshToken);

            _logger.LogInformation("User {UserId} ({Username}) logged in",
                discordUser.Id, discordUser.Username);

            return Ok(new TokenResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiry: expiry,
                User: discordUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth callback with code {Code}", code);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest? body,
        CancellationToken ct)
    {
        var token = Request.Cookies["refresh_token"] ?? body?.RefreshToken;

        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { error = "Refresh token is missing." });

        var claims = await _jwt.ValidateRefreshTokenAsync(token, ct);
        if (claims is null)
            return Unauthorized(new { error = "Refresh token is invalid or expired." });

        await _jwt.RevokeRefreshTokenAsync(token, ct);

        var (accessToken, expiry) = _jwt.GenerateAccessToken(
            claims.DiscordUserId, claims.Username, claims.Avatar);

        var newRefreshToken = await _jwt.GenerateRefreshTokenAsync(
            claims.DiscordUserId, claims.Username, claims.Avatar, ct);

        SetRefreshTokenCookie(newRefreshToken);

        return Ok(new { accessToken, refreshToken = newRefreshToken, accessTokenExpiry = expiry });
    }

    /// <summary>
    /// POST /auth/logout
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest? body,
        CancellationToken ct)
    {
        var token = Request.Cookies["refresh_token"] ?? body?.RefreshToken;

        if (!string.IsNullOrEmpty(token))
            await _jwt.RevokeRefreshTokenAsync(token, ct);

        Response.Cookies.Delete("refresh_token");
        Response.Cookies.Delete("access_token");

        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// GET /auth/me
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        var username = User.FindFirst("username")?.Value;
        var avatar   = User.FindFirst("avatar")?.Value;

        return Ok(new { userId, username, avatar });
    }

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Lax,
            Expires  = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}

/// <summary>
/// Request body cho POST /api/auth/discord (SPA flow)
/// </summary>
public record SpaLoginRequest(string Code, string? RedirectUri = null);