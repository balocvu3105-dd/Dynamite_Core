// src/Dynamite.API/Controllers/WelcomeController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.DTOs.Welcome;
using Dynamite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/guilds/{guildId}/welcome")]
[Authorize]
public class WelcomeController : ControllerBase
{
    private readonly IGuildConfigService _guildConfig;

    public WelcomeController(IGuildConfigService guildConfig)
    {
        _guildConfig = guildConfig;
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/welcome
    /// Returns the welcome + verification configuration.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWelcome(string guildId, CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var config = await _guildConfig.GetOrCreateConfigAsync(guildIdUlong, string.Empty, ct);

        return Ok(new WelcomeConfigDto(
            WelcomeEnabled:   config.WelcomeEnabled,
            WelcomeChannelId: config.WelcomeChannelId?.ToString(),
            WelcomeMessage:   config.WelcomeMessage,
            VerifyChannelId:  config.VerifyChannelId?.ToString(),
            VerifyRoleId:     config.VerifyRoleId?.ToString()
        ));
    }

    /// <summary>
    /// PATCH /api/guilds/{guildId}/welcome
    /// Update welcome + verification settings. Send "" to clear a channel/role.
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> UpdateWelcome(
        string guildId,
        [FromBody] UpdateWelcomeConfigRequest request,
        CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var config = await _guildConfig.GetOrCreateConfigAsync(guildIdUlong, string.Empty, ct);

        if (request.WelcomeEnabled.HasValue)
            config.WelcomeEnabled = request.WelcomeEnabled.Value;

        if (request.WelcomeMessage is not null)
            config.WelcomeMessage = string.IsNullOrEmpty(request.WelcomeMessage)
                ? null
                : request.WelcomeMessage;

        if (request.WelcomeChannelId is not null)
            config.WelcomeChannelId = ulong.TryParse(request.WelcomeChannelId, out var wc) ? wc : null;

        if (request.VerifyChannelId is not null)
            config.VerifyChannelId = ulong.TryParse(request.VerifyChannelId, out var vc) ? vc : null;

        if (request.VerifyRoleId is not null)
            config.VerifyRoleId = ulong.TryParse(request.VerifyRoleId, out var vr) ? vr : null;

        await _guildConfig.UpdateConfigAsync(config, ct);

        return Ok(new { message = "Welcome configuration updated." });
    }
}