// src/Dynamite.Core/Entities/UserBlacklist.cs
namespace Dynamite.Core.Entities;

/// <summary>
/// Persistent blacklist entry for a user in a guild.
/// Data survives even after the user leaves Discord — this is the key
/// difference from a plain Discord ban, which can be lifted or lost.
/// When a blacklisted user attempts to rejoin, the bot auto-bans them immediately.
/// </summary>
public class UserBlacklist : BaseEntity
{
    public ulong GuildId { get; set; }

    // ── Cached user info at time of blacklist ─────────────────────────────────
    // The user may have left, changed username, or deleted their account by the
    // time someone queries this record — so we snapshot these at write time.
    public ulong TargetUserId { get; set; }
    public string TargetUsername { get; set; } = string.Empty;
    public string? TargetAvatarUrl { get; set; }

    // ── Blacklist metadata ────────────────────────────────────────────────────
    public ulong ModeratorId { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Optional long-form notes (alt accounts, context, etc.).</summary>
    public string? Notes { get; set; }

    // ── Soft-removal ─────────────────────────────────────────────────────────
    public bool IsActive { get; set; } = true;
    public DateTime? RemovedAt { get; set; }
    public ulong? RemovedByModeratorId { get; set; }
    public string? RemoveReason { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;
}
