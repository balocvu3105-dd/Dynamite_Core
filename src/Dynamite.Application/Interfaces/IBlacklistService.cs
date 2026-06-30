// src/Dynamite.Application/Interfaces/IBlacklistService.cs
namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;

public interface IBlacklistService
{
    /// <summary>
    /// Add a user to the guild blacklist.
    /// Caller is responsible for issuing the Discord ban separately.
    /// </summary>
    Task<UserBlacklist> AddAsync(
        ulong guildId, string guildName,
        ulong targetId, string targetUsername, string? targetAvatarUrl,
        ulong moderatorId, string reason, string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Remove (soft-delete) a user from the guild blacklist.
    /// Throws KeyNotFoundException if no active entry exists.
    /// </summary>
    Task<UserBlacklist> RemoveAsync(
        ulong guildId, ulong targetId,
        ulong moderatorId, string reason,
        CancellationToken ct = default);

    Task<UserBlacklist?> GetAsync(ulong guildId, ulong targetId, CancellationToken ct = default);

    Task<IEnumerable<UserBlacklist>> GetAllAsync(ulong guildId, int count = 50, CancellationToken ct = default);

    Task<bool> IsBlacklistedAsync(ulong guildId, ulong targetId, CancellationToken ct = default);
}
