namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Enums;

/// <summary>
/// Repository for ModerationAction records.
///
/// IMMUTABILITY POLICY:
/// ModerationAction records are an append-only audit log.
/// Once written, a record MUST NOT be updated or deleted.
/// This guarantees a tamper-proof history of all moderation actions
/// (bans, kicks, blacklists, etc.) even after users leave the server.
///
/// The UpdateAsync / DeleteAsync methods inherited from IRepository
/// must NOT be called on this type in normal application code.
/// </summary>
public interface IModerationRepository : IRepository<ModerationAction>
{
    /// <summary>
    /// All actions recorded against a specific user in a guild.
    /// Works even if the user has left the server — data is persisted by user ID.
    /// </summary>
    Task<IEnumerable<ModerationAction>> GetUserHistoryAsync(
        ulong guildId, ulong userId,
        int count = 20, CancellationToken ct = default);

    /// <summary>All actions performed by a specific moderator in a guild.</summary>
    Task<IEnumerable<ModerationAction>> GetByModeratorAsync(
        ulong guildId, ulong moderatorId,
        int count = 20, CancellationToken ct = default);

    /// <summary>
    /// Most recent N actions in a guild, optionally filtered by action type.
    /// Useful for mod dashboards and audit views.
    /// </summary>
    Task<IEnumerable<ModerationAction>> GetRecentActionsAsync(
        ulong guildId, int count = 10,
        ModerationActionType? type = null,
        CancellationToken ct = default);

    /// <summary>
    /// All ban-type actions for a user across the guild history:
    /// Ban, BanId, Blacklist, Unban, Unblacklist.
    /// This is the primary query for "what happened to this user?"
    /// </summary>
    Task<IEnumerable<ModerationAction>> GetBanHistoryAsync(
        ulong guildId, ulong userId,
        CancellationToken ct = default);
}
