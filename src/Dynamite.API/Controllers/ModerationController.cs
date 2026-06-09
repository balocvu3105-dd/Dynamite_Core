// src/Dynamite.API/Controllers/ModerationController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.DTOs.Moderation;
using Dynamite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/guilds/{guildId}")]
[Authorize]
public class ModerationController : ControllerBase
{
    private readonly IModerationService _moderation;

    public ModerationController(IModerationService moderation)
    {
        _moderation = moderation;
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/warnings?userId=xxx&page=1&pageSize=20
    /// </summary>
    [HttpGet("warnings")]
    public async Task<IActionResult> GetWarnings(
        string guildId,
        [FromQuery] string? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var userIdUlong = ulong.TryParse(userId, out var uid) ? uid : 0UL;

        var all = await _moderation.GetWarningsAsync(guildIdUlong, userIdUlong, ct);

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var list = all.ToList();
        var items = list
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WarningDto(
                Id:          w.Id.ToString(),
                UserId:      w.TargetUserId.ToString(),
                ModeratorId: w.ModeratorId.ToString(),
                Reason:      w.Reason,
                CreatedAt:   w.CreatedAt));

        return Ok(new PagedResult<WarningDto>(
            Items:    items,
            Total:    list.Count,
            Page:     page,
            PageSize: pageSize));
    }

    /// <summary>
    /// DELETE /api/guilds/{guildId}/warnings/{warningId}
    /// Soft-deletes a warning (marks IsActive = false).
    /// Scoped to guild — cannot delete warnings from other guilds.
    /// </summary>
    [HttpDelete("warnings/{warningId}")]
    public async Task<IActionResult> DeleteWarning(
        string guildId,
        string warningId,
        CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        if (!Guid.TryParse(warningId, out var warningGuid))
            return BadRequest(new { error = "Invalid warning ID." });

        try
        {
            await _moderation.DeleteWarningAsync(guildIdUlong, warningGuid, ct);
            return Ok(new { message = "Warning deleted." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Warning not found." });
        }
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/modlogs?userId=xxx&page=1&pageSize=20
    /// </summary>
    [HttpGet("modlogs")]
    public async Task<IActionResult> GetModLogs(
        string guildId,
        [FromQuery] string? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var userIdUlong = ulong.TryParse(userId, out var uid) ? uid : 0UL;

        IEnumerable<Dynamite.Core.Entities.ModerationAction> all;

        if (userIdUlong == 0UL)
        {
            // No userId filter — return recent actions for the whole guild
            all = await _moderation.GetHistoryAsync(guildIdUlong, 0UL, ct);
        }
        else
        {
            all = await _moderation.GetHistoryAsync(guildIdUlong, userIdUlong, ct);
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var list = all.ToList();
        var items = list
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ModLogDto(
                Id:           a.Id.ToString(),
                Action:       a.ActionType.ToString().ToLowerInvariant(),
                TargetUserId: a.TargetUserId.ToString(),
                ModeratorId:  a.ModeratorId.ToString(),
                Reason:       a.Reason,
                CreatedAt:    a.CreatedAt));

        return Ok(new PagedResult<ModLogDto>(
            Items:    items,
            Total:    list.Count,
            Page:     page,
            PageSize: pageSize));
    }
}