// src/Dynamite.API/Auth/DiscordOAuthService.cs
namespace Dynamite.API.Auth;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dynamite.API.DTOs.Auth;
using Dynamite.API.DTOs.Guild;

public class DiscordOAuthService
{
    private const string DiscordApiBase = "https://discord.com/api/v10";
    private const string TokenEndpoint = "https://discord.com/api/oauth2/token";
    private const long ManageGuildPermission = 0x20;

    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public DiscordOAuthService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
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

    public async Task<string> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _config["Discord:ClientId"]!,
            ["client_secret"] = _config["Discord:ClientSecret"]!,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _config["Discord:RedirectUri"]!,
        });

        var response = await _http.PostAsync(TokenEndpoint, body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<DiscordTokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Discord token response.");

        return result.AccessToken;
    }

    public async Task<DiscordUserDto> GetCurrentUserAsync(
        string discordAccessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{DiscordApiBase}/users/@me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", discordAccessToken);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var raw = JsonSerializer.Deserialize<DiscordUserRaw>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Discord user.");

        return new DiscordUserDto(
            Id: raw.Id,
            Username: raw.Username,
            Avatar: raw.Avatar is not null
                ? $"https://cdn.discordapp.com/avatars/{raw.Id}/{raw.Avatar}.png"
                : null,
            Email: raw.Email);
    }

    public async Task<IEnumerable<DiscordGuildDto>> GetManageableGuildsAsync(
        string discordAccessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{DiscordApiBase}/users/@me/guilds");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", discordAccessToken);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var guilds = JsonSerializer.Deserialize<List<DiscordGuildRaw>>(json) ?? [];

        return guilds
            // Discord trả permissions là string — parse về long trước khi check
            .Where(g => long.TryParse(g.Permissions, out var perms) && (perms & ManageGuildPermission) != 0)
            .Select(g => new DiscordGuildDto(
                Id: g.Id,
                Name: g.Name,
                Icon: g.Icon,
                Permissions: long.TryParse(g.Permissions, out var p) ? p : 0));
    }

    public async Task<DiscordGuildDetailRaw?> GetGuildDetailAsync(
        string discordAccessToken,
        string guildId,
        CancellationToken ct = default)
    {
        using var chanReq = new HttpRequestMessage(
            HttpMethod.Get, $"{DiscordApiBase}/guilds/{guildId}/channels");
        chanReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", discordAccessToken);

        var chanResp = await _http.SendAsync(chanReq, ct);
        if (!chanResp.IsSuccessStatusCode) return null;

        var chanJson = await chanResp.Content.ReadAsStringAsync(ct);
        var channels = JsonSerializer.Deserialize<List<DiscordChannelRaw>>(chanJson) ?? [];

        using var guildReq = new HttpRequestMessage(
            HttpMethod.Get, $"{DiscordApiBase}/guilds/{guildId}");
        guildReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", discordAccessToken);

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
    [property: JsonPropertyName("permissions")] string Permissions);

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