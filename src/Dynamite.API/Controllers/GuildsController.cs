// src/Dynamite.API/Controllers/GuildsController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.Auth;
using Dynamite.API.DTOs.Auth;
using Dynamite.API.DTOs.Guild;
using Dynamite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/guilds")]
[Authorize]
public class GuildsController : ControllerBase
{
    private readonly DiscordOAuthService _discord;
    private readonly GuildAuthorizationService _guildAuth;
    private readonly IGuildConfigService _guildConfig;
    private readonly ILogger<GuildsController> _logger;

    public GuildsController(
        DiscordOAuthService discord,
        GuildAuthorizationService guildAuth,
        IGuildConfigService guildConfig,
        ILogger<GuildsController> logger)
    {
        _discord = discord;
        _guildAuth = guildAuth;
        _guildConfig = guildConfig;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/guilds
    /// Trả về danh sách guilds user có thể manage.
    /// Cần Discord access token — client phải gửi kèm header X-Discord-Token.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGuilds(
        [FromHeader(Name = "X-Discord-Token")] string? discordToken,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(discordToken))
            return BadRequest(new { error = "X-Discord-Token header is required." });

        var guilds = await _discord.GetManageableGuildsAsync(discordToken, ct);

        var result = guilds.Select(g => new GuildSummaryDto(
            Id: g.Id,
            Name: g.Name,
            IconUrl: g.Icon is not null
                ? $"https://cdn.discordapp.com/icons/{g.Id}/{g.Icon}.png"
                : null,
            BotPresent: true // Phase 9b: check thực từ bot's guild list
        ));

        return Ok(result);
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/settings
    /// </summary>
    [HttpGet("{guildId}/settings")]
    public async Task<IActionResult> GetSettings(
        string guildId,
        CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var config = await _guildConfig.GetOrCreateConfigAsync(guildIdUlong, string.Empty);

        return Ok(new GuildSettingsDto(
            GuildId: config.GuildId.ToString(),
            GuildName: config.GuildName,
            ModerationEnabled: config.ModerationEnabled,
            WelcomeEnabled: config.WelcomeEnabled,
            LoggingEnabled: config.LoggingEnabled,
            AutoRoleEnabled: config.AutoRoleEnabled,
            ModLogChannelId: config.ModLogChannelId?.ToString(),
            WelcomeChannelId: config.WelcomeChannelId?.ToString(),
            WelcomeMessage: config.WelcomeMessage,
            VerifyChannelId: config.VerifyChannelId?.ToString(),
            VerifyRoleId: config.VerifyRoleId?.ToString()
        ));
    }

    /// <summary>
    /// PATCH /api/guilds/{guildId}/settings
    /// Chỉ update các field được gửi lên (nullable = không thay đổi).
    /// </summary>
    [HttpPatch("{guildId}/settings")]
    public async Task<IActionResult> UpdateSettings(
        string guildId,
        [FromBody] UpdateGuildSettingsRequest request,
        CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var config = await _guildConfig.GetOrCreateConfigAsync(guildIdUlong, string.Empty);

        // Chỉ update field nào client gửi lên
        if (request.ModerationEnabled.HasValue)
            config.ModerationEnabled = request.ModerationEnabled.Value;
        if (request.WelcomeEnabled.HasValue)
            config.WelcomeEnabled = request.WelcomeEnabled.Value;
        if (request.LoggingEnabled.HasValue)
            config.LoggingEnabled = request.LoggingEnabled.Value;
        if (request.AutoRoleEnabled.HasValue)
            config.AutoRoleEnabled = request.AutoRoleEnabled.Value;
        if (request.ModLogChannelId is not null)
            config.ModLogChannelId = ulong.TryParse(request.ModLogChannelId, out var v) ? v : null;
        if (request.WelcomeChannelId is not null)
            config.WelcomeChannelId = ulong.TryParse(request.WelcomeChannelId, out var v2) ? v2 : null;
        if (request.WelcomeMessage is not null)
            config.WelcomeMessage = request.WelcomeMessage;
        if (request.VerifyChannelId is not null)
            config.VerifyChannelId = ulong.TryParse(request.VerifyChannelId, out var v3) ? v3 : null;
        if (request.VerifyRoleId is not null)
            config.VerifyRoleId = ulong.TryParse(request.VerifyRoleId, out var v4) ? v4 : null;

        await _guildConfig.UpdateConfigAsync(config);

        return Ok(new { message = "Settings updated successfully." });
    }
}