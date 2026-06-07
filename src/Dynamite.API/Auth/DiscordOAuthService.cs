// src/Dynamite.API/Auth/DiscordOAuthService.cs
namespace Dynamite.API.Auth;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dynamite.API.DTOs.Auth;

public class DiscordOAuthService
{
    private const string DiscordApiBase = "https://discord.com/api/v10";
    private const string TokenEndpoint = "https://discord.com/api/oauth2/token";

    // ManageGuild permission bit — dùng để filter guilds
    private const long ManageGuildPermission = 0x20;

    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public DiscordOAuthService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    /// <summary>
    /// Bước 1: Exchange authorization code lấy Discord access token.
    /// Code này chỉ dùng được một lần — Discord gửi qua redirect URL.
    /// </summary>
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

    /// <summary>
    /// Bước 2: Dùng Discord access token để lấy thông tin user.
    /// </summary>
    public async Task<DiscordUserDto> GetCurrentUserAsync(
        string discordAccessToken,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{DiscordApiBase}/users/@me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", discordAccessToken);

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

    /// <summary>
    /// Bước 3: Lấy danh sách guilds mà user có ManageGuild permission.
    /// Đây là danh sách guilds user có thể config trên dashboard.
    /// </summary>
    public async Task<IEnumerable<DiscordGuildDto>> GetManageableGuildsAsync(
        string discordAccessToken,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{DiscordApiBase}/users/@me/guilds");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", discordAccessToken);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var guilds = JsonSerializer.Deserialize<List<DiscordGuildDto>>(json)
            ?? [];

        // Chỉ trả về guilds user có quyền ManageGuild
        return guilds.Where(g => (g.Permissions & ManageGuildPermission) != 0);
    }

    public string BuildOAuthUrl()
    {
        var clientId = _config["Discord:ClientId"]!;
        var redirectUri = Uri.EscapeDataString(_config["Discord:RedirectUri"]!);
        const string scope = "identify guilds";

        return $"https://discord.com/oauth2/authorize"
             + $"?client_id={clientId}"
             + $"&redirect_uri={redirectUri}"
             + $"&response_type=code"
             + $"&scope={Uri.EscapeDataString(scope)}";
    }

    // ─── Internal deserialization models ───────────────────────────────────

    private record DiscordTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);

    private record DiscordUserRaw(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("avatar")] string? Avatar,
        [property: JsonPropertyName("email")] string? Email);
}