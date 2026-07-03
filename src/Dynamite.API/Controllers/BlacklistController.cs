// src/Dynamite.API/Controllers/BlacklistController.cs
namespace Dynamite.API.Controllers;

using Dynamite.Application.Interfaces;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Dynamite.API.Filters;

public record BlacklistEntryDto(
    string UserId,
    string Username,
    string? AvatarUrl,
    string Reason,
    string? Notes,
    bool IsActive,
    string ModeratorId,
    DateTime CreatedAt);

public record AddBlacklistRequestDto(
    string TargetUserId,
    string TargetUsername,
    string? TargetAvatarUrl,
    string Reason,
    string? Notes);

public record RemoveBlacklistRequestDto(string Reason);

[ApiController]
[Route("api/guilds/{guildId}/blacklist")]
[Authorize]
[RequireGuildAdmin]
public class BlacklistController : ControllerBase
{
    private readonly IBlacklistService _blacklistService;
    private readonly IGuildConfigRepository _guildConfigRepo;

    public BlacklistController(IBlacklistService blacklistService, IGuildConfigRepository guildConfigRepo)
    {
        _blacklistService = blacklistService;
        _guildConfigRepo = guildConfigRepo;
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/blacklist — list active blacklist entries
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBlacklist(
        string guildId,
        [FromQuery] int count = 50,
        CancellationToken ct = default)
    {
        if (!ulong.TryParse(guildId, out var gid))
            return BadRequest(new { error = "Invalid guild ID." });

        var entries = await _blacklistService.GetAllAsync(gid, count, ct);

        var result = entries.Select(e => new BlacklistEntryDto(
            UserId:       e.TargetUserId.ToString(),
            Username:     e.TargetUsername,
            AvatarUrl:    e.TargetAvatarUrl,
            Reason:       e.Reason,
            Notes:        e.Notes,
            IsActive:     e.IsActive,
            ModeratorId:  e.ModeratorId.ToString(),
            CreatedAt:    e.CreatedAt));

        return Ok(result);
    }

    /// <summary>
    /// POST /api/guilds/{guildId}/blacklist — add a user to blacklist
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToBlacklist(
        string guildId,
        [FromBody] AddBlacklistRequestDto request,
        CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var gid))
            return BadRequest(new { error = "Invalid guild ID." });

        if (!ulong.TryParse(request.TargetUserId, out var targetId))
            return BadRequest(new { error = "Invalid target user ID." });

        var moderatorId = ulong.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out var mid) ? mid : 0UL;

        // Resolve guild name from stored config (guild ID stored as ulong, name cached at bot join)
        var guildConfig = await _guildConfigRepo.GetByGuildIdAsync(gid);
        var guildName = guildConfig?.GuildName ?? guildId;

        try
        {
            var entry = await _blacklistService.AddAsync(
                gid, guildName,
                targetId, request.TargetUsername, request.TargetAvatarUrl,
                moderatorId, request.Reason, request.Notes, ct);

            return Ok(new BlacklistEntryDto(
                UserId:      entry.TargetUserId.ToString(),
                Username:    entry.TargetUsername,
                AvatarUrl:   entry.TargetAvatarUrl,
                Reason:      entry.Reason,
                Notes:       entry.Notes,
                IsActive:    entry.IsActive,
                ModeratorId: entry.ModeratorId.ToString(),
                CreatedAt:   entry.CreatedAt));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/guilds/{guildId}/blacklist/{userId} — remove user from blacklist
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> RemoveFromBlacklist(
        string guildId,
        string userId,
        [FromBody] RemoveBlacklistRequestDto request,
        CancellationToken ct)
    {
        if (!ulong.TryParse(guildId, out var gid))
            return BadRequest(new { error = "Invalid guild ID." });

        if (!ulong.TryParse(userId, out var targetId))
            return BadRequest(new { error = "Invalid user ID." });

        var moderatorId = ulong.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out var mid) ? mid : 0UL;

        try
        {
            await _blacklistService.RemoveAsync(gid, targetId, moderatorId, request.Reason, ct);
            return Ok(new { message = "User removed from blacklist." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "No active blacklist entry found for this user." });
        }
    }
}
