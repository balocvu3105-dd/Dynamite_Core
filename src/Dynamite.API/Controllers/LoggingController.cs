// src/Dynamite.API/Controllers/LoggingController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.DTOs.Logging;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/guilds/{guildId}/logging")]
[Authorize]
public class LoggingController : ControllerBase
{
    private readonly IServerLogService _logService;
    private readonly IGuildConfigService _guildConfig;

    public LoggingController(IServerLogService logService, IGuildConfigService guildConfig)
    {
        _logService = logService;
        _guildConfig = guildConfig;
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/logging
    /// Returns the configured log channel IDs for all categories.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogging(string guildId, CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var msg    = await _logService.GetLogChannelAsync(guildIdUlong, LogCategory.Message, ct);
        var member = await _logService.GetLogChannelAsync(guildIdUlong, LogCategory.Member, ct);
        var voice  = await _logService.GetLogChannelAsync(guildIdUlong, LogCategory.Voice, ct);
        var server = await _logService.GetLogChannelAsync(guildIdUlong, LogCategory.Server, ct);

        return Ok(new LoggingConfigDto(
            MessageLogChannelId: msg?.ToString(),
            MemberLogChannelId:  member?.ToString(),
            VoiceLogChannelId:   voice?.ToString(),
            ServerLogChannelId:  server?.ToString()
        ));
    }

    /// <summary>
    /// PATCH /api/guilds/{guildId}/logging
    /// Update one or more log channels. Send "" to clear a channel.
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> UpdateLogging(
        string guildId,
        [FromBody] UpdateLoggingConfigRequest request,
        CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Message,  request.MessageLogChannelId, ct);
        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Member,   request.MemberLogChannelId, ct);
        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Voice,    request.VoiceLogChannelId, ct);
        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Server,   request.ServerLogChannelId, ct);

        return Ok(new { message = "Logging configuration updated." });
    }

    // Centralised logic: null = skip, "" = clear, valid ID = set
    private async Task ApplyChannelUpdateAsync(
        ulong guildId, LogCategory category, string? value, CancellationToken ct)
    {
        if (value is null) return; // field not sent in request — skip

        if (value == string.Empty)
        {
            await _logService.ClearLogChannelAsync(guildId, string.Empty, category, ct);
            return;
        }

        if (ulong.TryParse(value, out var channelId))
            await _logService.SetLogChannelAsync(guildId, string.Empty, category, channelId, ct);
    }
}