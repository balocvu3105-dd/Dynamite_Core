// src/Dynamite.Application/Interfaces/IModerationService.cs
namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;

/// <summary>
/// AUDIT POLICY — all ban-type actions (Ban, BanId, Unban, Blacklist, Unblacklist)
/// are written as immutable records to ModerationActions. Records are NEVER
/// updated or deleted. Reason is REQUIRED and validated non-empty before writing.
/// </summary>
public interface IModerationService
{
    Task<ModerationAction> WarnAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, string targetUsername = "", string moderatorUsername = "", CancellationToken ct = default);
    Task<ModerationAction> KickAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, string targetUsername = "", string moderatorUsername = "", CancellationToken ct = default);

    /// <summary>
    /// Ban a member who is currently in the server.
    /// Reason is mandatory — throws ArgumentException if null or whitespace.
    /// </summary>
    Task<ModerationAction> BanAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, string targetUsername = "", string moderatorUsername = "", CancellationToken ct = default);

    /// <summary>
    /// Ban a user by Discord ID (user does not need to be in the server).
    /// Reason is mandatory — throws ArgumentException if null or whitespace.
    /// </summary>
    Task<ModerationAction> BanByIdAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, string targetUsername = "", string moderatorUsername = "", CancellationToken ct = default);

    /// <summary>
    /// Unban a previously banned user.
    /// Reason is mandatory — throws ArgumentException if null or whitespace.
    /// </summary>
    Task<ModerationAction> UnbanAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, string targetUsername = "", string moderatorUsername = "", CancellationToken ct = default);

    Task<ModerationAction> TimeoutAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, TimeSpan duration, string targetUsername = "", string moderatorUsername = "", CancellationToken ct = default);
    Task<ModerationAction> UntimeoutAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string targetUsername = "", string moderatorUsername = "", CancellationToken ct = default);
    Task<IEnumerable<Warning>> GetWarningsAsync(ulong guildId, ulong userId, CancellationToken ct = default);
    Task<IEnumerable<ModerationAction>> GetHistoryAsync(ulong guildId, ulong userId, CancellationToken ct = default);

    /// <summary>
    /// Full ban audit history for a user (Ban, BanId, Unban, Blacklist, Unblacklist).
    /// Works even if the user has left the server — queries by stored user ID.
    /// </summary>
    Task<IEnumerable<ModerationAction>> GetBanHistoryAsync(ulong guildId, ulong userId, CancellationToken ct = default);

    /// <summary>
    /// Delete a warning by ID, scoped to a guild.
    /// Throws KeyNotFoundException if not found.
    /// </summary>
    Task DeleteWarningAsync(ulong guildId, Guid warningId, CancellationToken ct = default);
}
