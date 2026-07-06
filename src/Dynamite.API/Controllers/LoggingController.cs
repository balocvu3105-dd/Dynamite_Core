// src/Dynamite.API/Controllers/LoggingController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.DTOs.Logging;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dynamite.API.Filters;

[ApiController]
[Route("api/guilds/{guildId}/logging")]
[Authorize]
[RequireGuildAdmin]
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
        var mod    = await _logService.GetLogChannelAsync(guildIdUlong, LogCategory.Moderation, ct);
        var audit  = await _logService.GetLogChannelAsync(guildIdUlong, LogCategory.Audit, ct);

        return Ok(new LoggingConfigDto(
            MessageLogChannelId: msg?.ToString(),
            MemberLogChannelId:  member?.ToString(),
            VoiceLogChannelId:   voice?.ToString(),
            ServerLogChannelId:  server?.ToString(),
            ModLogChannelId:     mod?.ToString(),
            AuditLogChannelId:   audit?.ToString()
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

        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Message,    request.MessageLogChannelId, ct);
        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Member,     request.MemberLogChannelId, ct);
        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Voice,      request.VoiceLogChannelId, ct);
        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Server,     request.ServerLogChannelId, ct);
        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Moderation, request.ModLogChannelId, ct);
        await ApplyChannelUpdateAsync(guildIdUlong, LogCategory.Audit,      request.AuditLogChannelId, ct);

        return Ok(new { message = "Logging configuration updated." });
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/logging/activities — get activity logs with pagination and search
    /// </summary>
    [HttpGet("activities")]
    public async Task<IActionResult> GetActivityLogs(
        string guildId,
        [FromQuery] LogCategory? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var (logs, totalCount) = await _logService.GetActivityLogsAsync(
            guildIdUlong, category, search, page, pageSize, ct);

        var result = logs.Select(l => new
        {
            l.Id,
            GuildId = l.GuildId.ToString(),
            l.Category,
            l.EventType,
            l.Title,
            l.Description,
            l.ActorId,
            l.ActorUsername,
            l.ActorAvatarUrl,
            l.TargetId,
            l.TargetUsername,
            l.Metadata,
            l.CreatedAt
        });

        return Ok(new { logs = result, totalCount, page, pageSize });
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