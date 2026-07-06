// src/Dynamite.Application/Interfaces/IServerLogService.cs
namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;
using Dynamite.Core.Enums;

public interface IServerLogService
{
    /// <summary>
    /// Returns the configured log channel ID for a given category in a guild.
    /// Returns null if the category is not configured.
    /// </summary>
    Task<ulong?> GetLogChannelAsync(
        ulong guildId,
        LogCategory category,
        CancellationToken ct = default);

    Task SetLogChannelAsync(
        ulong guildId,
        string guildName,
        LogCategory category,
        ulong channelId,
        CancellationToken ct = default);

    Task ClearLogChannelAsync(
        ulong guildId,
        string guildName,
        LogCategory category,
        CancellationToken ct = default);

    Task LogActivityAsync(
        ulong guildId,
        LogCategory category,
        string eventType,
        string title,
        string description,
        string? actorId = null,
        string? actorUsername = null,
        string? actorAvatarUrl = null,
        string? targetId = null,
        string? targetUsername = null,
        string? metadata = null,
        CancellationToken ct = default);

    Task<(IEnumerable<ServerActivityLog> Logs, int TotalCount)> GetActivityLogsAsync(
        ulong guildId,
        LogCategory? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);
}