// src/Dynamite.API/DTOs/Moderation/ModerationDto.cs
namespace Dynamite.API.DTOs.Moderation;

public record WarningDto(
    string Id,
    string UserId,
    string? TargetUsername,
    string ModeratorId,
    string Reason,
    DateTime CreatedAt
);

public record ModLogDto(
    string Id,
    string Action,       // "ban", "kick", "timeout", "warn"
    string TargetUserId,
    string? TargetUsername,
    string ModeratorId,
    string? Reason,
    DateTime CreatedAt
);

/// <summary>
/// Wrapper cho paginated responses.
/// </summary>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int Total,
    int Page,
    int PageSize
);