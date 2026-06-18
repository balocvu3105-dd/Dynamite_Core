// src/Dynamite.API/Auth/JwtService.cs
namespace Dynamite.API.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dynamite.Core.Entities;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public class JwtService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public JwtService(IConfiguration config, AppDbContext db)
    {
        _config = config;
        _db = db;
    }

    // ─────────────────────────────────────────────
    // ACCESS TOKEN — stateless, 15 phút
    // ─────────────────────────────────────────────

    public (string Token, DateTime Expiry) GenerateAccessToken(
        string discordUserId,
        string username,
        string? avatar)
    {
        var secret = _config["Jwt:Secret"]!;
        var issuer = _config["Jwt:Issuer"]!;
        var audience = _config["Jwt:Audience"]!;
        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_config["Jwt:AccessTokenExpiryMinutes"]!));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  discordUserId),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
            new Claim("username", username),
            new Claim("avatar",   avatar ?? string.Empty),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    // ─────────────────────────────────────────────
    // REFRESH TOKEN — lưu DB, 7 ngày
    // ─────────────────────────────────────────────

    public async Task<string> GenerateRefreshTokenAsync(
        string discordUserId,
        string username,
        string? avatar,
        CancellationToken ct = default)
    {
        // Revoke tất cả token cũ của user này trước khi tạo mới
        // Tránh tích lũy token stale trong DB
        await RevokeAllUserTokensAsync(discordUserId, ct);

        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var tokenString = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            DiscordUserId = discordUserId,
            Username      = username,
            Avatar        = avatar,
            Token         = tokenString,
            ExpiresAt     = DateTime.UtcNow.AddDays(
                int.Parse(_config["Jwt:RefreshTokenExpiryDays"]!)),
            IsRevoked     = false
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        return tokenString;
    }

    /// <summary>
    /// Validates a refresh token and returns its stored claims,
    /// or null if the token is invalid/expired/revoked.
    /// </summary>
    public async Task<RefreshTokenClaims?> ValidateRefreshTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        var record = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        if (record is null || !record.IsActive)
            return null;

        return new RefreshTokenClaims(record.DiscordUserId, record.Username, record.Avatar);
    }

    public async Task RevokeRefreshTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        var record = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        if (record is null) return;

        record.IsRevoked = true;
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private async Task RevokeAllUserTokensAsync(
        string discordUserId,
        CancellationToken ct)
    {
        var tokens = await _db.RefreshTokens
            .Where(x => x.DiscordUserId == discordUserId && !x.IsRevoked)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.IsRevoked = true;

        if (tokens.Count > 0)
            await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Claims stored in a refresh token record, used to rebuild the access token
/// without needing to call Discord API on every refresh.
/// </summary>
public record RefreshTokenClaims(string DiscordUserId, string Username, string? Avatar);