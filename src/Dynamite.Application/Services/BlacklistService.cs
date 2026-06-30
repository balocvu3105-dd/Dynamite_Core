// src/Dynamite.Application/Services/BlacklistService.cs
namespace Dynamite.Application.Services;

using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class BlacklistService : IBlacklistService
{
    private readonly IBlacklistRepository _blacklistRepo;
    private readonly IGuildConfigRepository _guildConfigRepo;
    private readonly IModerationRepository _moderationRepo;
    private readonly ILogger<BlacklistService> _logger;

    public BlacklistService(
        IBlacklistRepository blacklistRepo,
        IGuildConfigRepository guildConfigRepo,
        IModerationRepository moderationRepo,
        ILogger<BlacklistService> logger)
    {
        _blacklistRepo = blacklistRepo;
        _guildConfigRepo = guildConfigRepo;
        _moderationRepo = moderationRepo;
        _logger = logger;
    }

    public async Task<UserBlacklist> AddAsync(
        ulong guildId, string guildName,
        ulong targetId, string targetUsername, string? targetAvatarUrl,
        ulong moderatorId, string reason, string? notes = null,
        CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "Blacklist reason cannot be empty. A documented reason is required.", nameof(reason));

        // Idempotent: if already blacklisted, return existing entry unchanged.
        var existing = await _blacklistRepo.GetActiveAsync(guildId, targetId, ct);
        if (existing is not null)
            return existing;

        var entry = new UserBlacklist
        {
            GuildId = guildId,
            TargetUserId = targetId,
            TargetUsername = targetUsername,
            TargetAvatarUrl = targetAvatarUrl,
            ModeratorId = moderatorId,
            Reason = reason,
            Notes = notes,
            GuildConfigId = config.Id
        };

        await _blacklistRepo.AddAsync(entry, ct);

        // Also write an audit trail entry in ModerationActions so the history
        // command shows it alongside bans, kicks, timeouts, etc.
        var auditAction = new ModerationAction
        {
            GuildId = guildId,
            TargetUserId = targetId,
            ModeratorId = moderatorId,
            ActionType = ModerationActionType.Blacklist,
            Reason = reason,
            GuildConfigId = config.Id
        };

        await _moderationRepo.AddAsync(auditAction, ct);
        await _blacklistRepo.SaveChangesAsync(ct); // single SaveChanges covers both

        _logger.LogInformation("User {TargetId} blacklisted in guild {GuildId} by {ModId}: {Reason}",
            targetId, guildId, moderatorId, reason);

        return entry;
    }

    public async Task<UserBlacklist> RemoveAsync(
        ulong guildId, ulong targetId,
        ulong moderatorId, string reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "Unblacklist reason cannot be empty. A documented reason is required.", nameof(reason));

        var entry = await _blacklistRepo.GetActiveAsync(guildId, targetId, ct)
            ?? throw new KeyNotFoundException(
                $"No active blacklist entry for user {targetId} in guild {guildId}.");

        entry.IsActive = false;
        entry.RemovedAt = DateTime.UtcNow;
        entry.RemovedByModeratorId = moderatorId;
        entry.RemoveReason = reason;
        entry.UpdatedAt = DateTime.UtcNow;

        await _blacklistRepo.UpdateAsync(entry, ct);

        // Audit trail
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, string.Empty, ct);
        var auditAction = new ModerationAction
        {
            GuildId = guildId,
            TargetUserId = targetId,
            ModeratorId = moderatorId,
            ActionType = ModerationActionType.Unblacklist,
            Reason = reason,
            GuildConfigId = config.Id
        };
        await _moderationRepo.AddAsync(auditAction, ct);
        await _blacklistRepo.SaveChangesAsync(ct);

        _logger.LogInformation("User {TargetId} removed from blacklist in guild {GuildId} by {ModId}",
            targetId, guildId, moderatorId);

        return entry;
    }

    public Task<UserBlacklist?> GetAsync(ulong guildId, ulong targetId, CancellationToken ct = default)
        => _blacklistRepo.GetActiveAsync(guildId, targetId, ct);

    public Task<IEnumerable<UserBlacklist>> GetAllAsync(ulong guildId, int count = 50, CancellationToken ct = default)
        => _blacklistRepo.GetAllActiveAsync(guildId, count, ct);

    public Task<bool> IsBlacklistedAsync(ulong guildId, ulong targetId, CancellationToken ct = default)
        => _blacklistRepo.IsBlacklistedAsync(guildId, targetId, ct);
}
