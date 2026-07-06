// src/Dynamite.API/Controllers/SecurityController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.DTOs.Security;
using Dynamite.Application.Interfaces;
using Dynamite.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/guilds/{guildId}/security")]
[Authorize]
[RequireGuildAdmin]
public class SecurityController : ControllerBase
{
    private readonly IAntiSpamService _antiSpam;

    public SecurityController(IAntiSpamService antiSpam)
    {
        _antiSpam = antiSpam;
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/security
    /// Returns the current anti-spam / security configuration.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSecurity(string guildId, CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var config = await _antiSpam.GetOrCreateConfigAsync(guildIdUlong, string.Empty, ct);

        return Ok(new SecurityConfigDto(
            Enabled:              config.Enabled,
            MessageThreshold:     config.MessageThreshold,
            MessageWindowSeconds: config.MessageWindowSeconds,
            MentionThreshold:     config.MentionThreshold,
            AntiInvite:           config.AntiInvite,
            AntiScamLink:         config.AntiScamLink,
            AntiRaid:             config.AntiRaid,
            RaidThreshold:        config.RaidThreshold
        ));
    }

    /// <summary>
    /// PATCH /api/guilds/{guildId}/security
    /// Update anti-spam settings. Only provided fields are updated.
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> UpdateSecurity(
        string guildId,
        [FromBody] UpdateSecurityConfigRequest request,
        CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        // Delegate to IAntiSpamService which already has per-field methods.
        // We call each one only if the field was included in the request.

        if (request.Enabled.HasValue)
            await _antiSpam.SetEnabledAsync(guildIdUlong, string.Empty, request.Enabled.Value, ct);

        if (request.MessageThreshold.HasValue && request.MessageWindowSeconds.HasValue)
            await _antiSpam.SetMessageThresholdAsync(
                guildIdUlong, string.Empty,
                request.MessageThreshold.Value,
                request.MessageWindowSeconds.Value, ct);

        if (request.MentionThreshold.HasValue)
            await _antiSpam.SetMentionThresholdAsync(
                guildIdUlong, string.Empty, request.MentionThreshold.Value, ct);

        if (request.AntiInvite.HasValue)
            await _antiSpam.SetFeatureAsync(guildIdUlong, string.Empty, "antiinvite", request.AntiInvite.Value, ct);

        if (request.AntiScamLink.HasValue)
            await _antiSpam.SetFeatureAsync(guildIdUlong, string.Empty, "antiscamlink", request.AntiScamLink.Value, ct);

        if (request.AntiRaid.HasValue)
            await _antiSpam.SetFeatureAsync(guildIdUlong, string.Empty, "antiraid", request.AntiRaid.Value, ct);

        if (request.RaidThreshold.HasValue)
            await _antiSpam.SetRaidThresholdAsync(
                guildIdUlong, string.Empty, request.RaidThreshold.Value, ct);

        return Ok(new { message = "Security configuration updated." });
    }
}