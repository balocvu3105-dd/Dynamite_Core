// src/Dynamite.API/Auth/DiscordOAuthService.cs
namespace Dynamite.API.Auth;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dynamite.API.DTOs.Auth;
using Dynamite.API.DTOs.Guild;
using Microsoft.Extensions.Caching.Memory;

public class DiscordOAuthService
{
    private const string DiscordApiBase = "https://discord.com/api/v10";
    private const string TokenEndpoint = "https://discord.com/api/oauth2/token";
    private const long AdministratorPermission = 0x8;

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    public DiscordOAuthService(HttpClient http, IConfiguration config, IMemoryCache cache)
    {
        _http = http;
        _config = config;
        _cache = cache;
    }

    /// <summary>
    /// Builds the Discord OAuth2 authorization URL.
    /// <paramref name="state"/> must be a cryptographically random, unguessable value
    /// generated per-request (CSRF protection). The caller is responsible for storing
    /// it (e.g. HttpOnly cookie) and verifying it matches the value Discord returns.
    /// </summary>
    public string BuildOAuthUrl(string state)
    {
        var clientId = _config["Discord:ClientId"]!;
        var redirectUri = Uri.EscapeDataString(_config["Discord:RedirectUri"]!);
        var scope = Uri.EscapeDataString("identify email guilds");
        var encodedState = Uri.EscapeDataString(state);
        return $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={encodedState}";
    }

    public async Task<string> ExchangeCodeAsync(string code, string? redirectUri = null, CancellationToken ct = default)
    {
        var targetRedirectUri = !string.IsNullOrWhiteSpace(redirectUri) ? redirectUri : _config["Discord:RedirectUri"]!;
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _config["Discord:ClientId"]!,
            ["client_secret"] = _config["Discord:ClientSecret"]!,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = targetRedirectUri,
        });

        var response = await _http.PostAsync(TokenEndpoint, body, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errJson = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Discord Token API Error: {response.StatusCode} - {errJson}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<DiscordTokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Discord token response.");

        return result.AccessToken;
    }

    public async Task<DiscordUserDto> GetCurrentUserAsync(
        string discordAccessToken, CancellationToken ct = default)
    {
        var cacheKey = $"CurrentUser_{discordAccessToken}";
        if (_cache.TryGetValue(cacheKey, out DiscordUserDto? cachedUser) && cachedUser != null)
        {
            return cachedUser;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{DiscordApiBase}/users/@me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", discordAccessToken);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var raw = JsonSerializer.Deserialize<DiscordUserRaw>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Discord user.");

        var result = new DiscordUserDto(
            Id: raw.Id,
            Username: raw.Username,
            Avatar: raw.Avatar is not null
                ? $"https://cdn.discordapp.com/avatars/{raw.Id}/{raw.Avatar}.png"
                : null,
            Email: raw.Email);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<DiscordUserDto?> GetUserByIdAsync(
        string userId, CancellationToken ct = default)
    {
        var botToken = _config["Discord:BotToken"] ?? _config["Discord:Token"];
        if (string.IsNullOrWhiteSpace(botToken)) return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{DiscordApiBase}/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        var raw = JsonSerializer.Deserialize<DiscordUserRaw>(json);
        if (raw is null) return null;

        return new DiscordUserDto(
            Id: raw.Id,
            Username: raw.Username,
            Avatar: raw.Avatar is not null
                ? $"https://cdn.discordapp.com/avatars/{raw.Id}/{raw.Avatar}.png"
                : null,
            Email: null);
    }

    public async Task<IEnumerable<DiscordGuildDto>> GetManageableGuildsAsync(
        string discordAccessToken, CancellationToken ct = default)
    {
        var cacheKey = $"ManageableGuilds_{discordAccessToken}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<DiscordGuildDto>? cachedGuilds) && cachedGuilds != null)
        {
            return cachedGuilds;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{DiscordApiBase}/users/@me/guilds");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", discordAccessToken);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var guilds = JsonSerializer.Deserialize<List<DiscordGuildRaw>>(json) ?? [];

        var result = guilds
            // Discord trả permissions là string — parse về long trước khi check Admin hoặc Owner
            .Where(g => g.Owner || (long.TryParse(g.Permissions, out var perms) && (perms & AdministratorPermission) != 0))
            .Select(g => new DiscordGuildDto(
                Id: g.Id,
                Name: g.Name,
                Icon: g.Icon,
                Permissions: long.TryParse(g.Permissions, out var p) ? p : 0,
                Owner: g.Owner))
            .ToList();

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<DiscordGuildDetailRaw?> GetGuildDetailAsync(
        string discordAccessToken,
        string guildId,
        CancellationToken ct = default)
    {
        var botToken = _config["Discord:BotToken"] ?? _config["Discord:Token"];
        if (string.IsNullOrWhiteSpace(botToken)) return null;

        using var chanReq = new HttpRequestMessage(
            HttpMethod.Get, $"{DiscordApiBase}/guilds/{guildId}/channels");
        chanReq.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);

        var chanResp = await _http.SendAsync(chanReq, ct);
        if (!chanResp.IsSuccessStatusCode) return null;

        var chanJson = await chanResp.Content.ReadAsStringAsync(ct);
        var channels = JsonSerializer.Deserialize<List<DiscordChannelRaw>>(chanJson) ?? [];

        using var guildReq = new HttpRequestMessage(
            HttpMethod.Get, $"{DiscordApiBase}/guilds/{guildId}");
        guildReq.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);

        var guildResp = await _http.SendAsync(guildReq, ct);
        if (!guildResp.IsSuccessStatusCode) return null;

        var guildJson = await guildResp.Content.ReadAsStringAsync(ct);
        var guildRaw = JsonSerializer.Deserialize<DiscordGuildFullRaw>(guildJson);
        if (guildRaw is null) return null;

        return new DiscordGuildDetailRaw(
            Name: guildRaw.Name,
            Icon: guildRaw.Icon,
            Channels: channels
                .Select(c => new DiscordChannelSimple(
                    Id: c.Id,
                    Name: c.Name,
                    Type: MapChannelType(c.Type)))
                .ToList(),
            Roles: guildRaw.Roles
                .Select(r => new DiscordRoleSimple(
                    Id: r.Id,
                    Name: r.Name,
                    Color: r.Color,
                    Managed: r.Managed))
                .ToList()
        );
    }

    private static string MapChannelType(int type) => type switch
    {
        0 => "text",
        2 => "voice",
        4 => "category",
        5 => "announcement",
        13 => "stage",
        15 => "forum",
        _ => "other"
    };
}

// ─── Internal raw DTOs ────────────────────────────────────────────────────────

file record DiscordTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken);

file record DiscordUserRaw(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("avatar")] string? Avatar,
    [property: JsonPropertyName("email")] string? Email);

// Fix: permissions từ Discord API là string, không phải long
file record DiscordGuildRaw(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("permissions")] string Permissions,
    [property: JsonPropertyName("owner")] bool Owner);

file record DiscordGuildFullRaw(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("roles")] List<DiscordRoleRaw> Roles);

file record DiscordRoleRaw(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("color")] int Color,
    [property: JsonPropertyName("managed")] bool Managed);

file record DiscordChannelRaw(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] int Type);

// Public records cho GuildsController
public record DiscordGuildDetailRaw(
    string Name,
    string? Icon,
    List<DiscordChannelSimple> Channels,
    List<DiscordRoleSimple> Roles);

public record DiscordChannelSimple(string Id, string Name, string Type);
public record DiscordRoleSimple(string Id, string Name, int Color, bool Managed);