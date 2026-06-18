// src/Dynamite.API/Controllers/GuildsController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.Auth;
using Dynamite.API.DTOs.Auth;
using Dynamite.API.DTOs.Guild;
using Dynamite.API.Services;
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
    private readonly GuildPresenceService _presence;
    private readonly ILogger<GuildsController> _logger;

    public GuildsController(
        DiscordOAuthService discord,
        GuildAuthorizationService guildAuth,
        IGuildConfigService guildConfig,
        GuildPresenceService presence,
        ILogger<GuildsController> logger)
    {
        _discord = discord;
        _guildAuth = guildAuth;
        _guildConfig = guildConfig;
        _presence = presence;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/guilds
    /// Trả về danh sách guilds user có thể manage + BotPresent thực.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGuilds(
        [FromHeader(Name = "X-Discord-Token")] string? discordToken,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(discordToken))
            return BadRequest(new { error = "X-Discord-Token header is required." });

        var guilds = await _discord.GetManageableGuildsAsync(discordToken, ct);
        var guildList = guilds.ToList();

        // Batch check BotPresent — 1 query duy nhất, không N+1
        var guildIds = guildList.Select(g => g.Id);
        var presentIds = await _presence.GetPresentGuildIdsAsync(guildIds, ct);

        var result = guildList.Select(g => new GuildSummaryDto(
            Id: g.Id,
            Name: g.Name,
            IconUrl: g.Icon is not null
                ? $"https://cdn.discordapp.com/icons/{g.Id}/{g.Icon}.png"
                : null,
            BotPresent: presentIds.Contains(g.Id)  // ← real check
        ));

        return Ok(result);
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/info
    /// Trả về channels + roles — dùng cho dashboard dropdowns.
    /// Gọi Discord REST bằng bot token để lấy live data.
    /// </summary>
    [HttpGet("{guildId}/info")]
    public async Task<IActionResult> GetGuildInfo(
        string guildId,
        [FromHeader(Name = "X-Discord-Token")] string? discordToken,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(discordToken))
            return BadRequest(new { error = "X-Discord-Token header is required." });

        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        // Kiểm tra bot có trong server không trước khi fetch
        var botPresent = await _presence.IsBotPresentAsync(guildIdUlong, ct);

        // Fetch guild details từ Discord REST (dùng user token)
        var guildDetail = await _discord.GetGuildDetailAsync(discordToken, guildId, ct);
        if (guildDetail is null)
            return NotFound(new { error = "Guild not found or access denied." });

        var channels = guildDetail.Channels.Select(c => new ChannelDto(
            Id: c.Id,
            Name: c.Name,
            Type: c.Type
        ));

        var roles = guildDetail.Roles
            .Where(r => r.Name != "@everyone")  // filter @everyone ra
            .Select(r => new RoleDto(
                Id: r.Id,
                Name: r.Name,
                Color: $"#{r.Color:X6}",
                IsManaged: r.Managed
            ));

        return Ok(new GuildInfoDto(
            Id: guildId,
            Name: guildDetail.Name,
            IconUrl: guildDetail.Icon is not null
                ? $"https://cdn.discordapp.com/icons/{guildId}/{guildDetail.Icon}.png"
                : null,
            BotPresent: botPresent,
            Channels: channels,
            Roles: roles
        ));
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/settings
    /// </summary>
    [HttpGet("{guildId}/settings")]
    public async Task<IActionResult> GetSettings(
        string guildId,
        [FromHeader(Name = "X-Discord-Token")] string? discordToken,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(discordToken))
            return BadRequest(new { error = "X-Discord-Token header is required." });

        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        // Authorization: verify caller has ManageGuild in this guild
        var guilds = await _discord.GetManageableGuildsAsync(discordToken, ct);
        if (_guildAuth.GetManageableGuild(guilds, guildId) is null)
            return Forbid();

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
    /// </summary>
    [HttpPatch("{guildId}/settings")]
    public async Task<IActionResult> UpdateSettings(
        string guildId,
        [FromBody] UpdateGuildSettingsRequest request,
        [FromHeader(Name = "X-Discord-Token")] string? discordToken,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(discordToken))
            return BadRequest(new { error = "X-Discord-Token header is required." });

        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        // Authorization: verify caller has ManageGuild in this guild
        var guilds = await _discord.GetManageableGuildsAsync(discordToken, ct);
        if (_guildAuth.GetManageableGuild(guilds, guildId) is null)
            return Forbid();

        var config = await _guildConfig.GetOrCreateConfigAsync(guildIdUlong, string.Empty);

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

        return Ok(new { message = "Settings updated." });
    }
}