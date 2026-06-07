// src/Dynamite.Application/Interfaces/IServerLogService.cs
namespace Dynamite.Application.Interfaces;

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
}