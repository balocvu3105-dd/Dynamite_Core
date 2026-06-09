// src/Dynamite.Application/Services/ModerationService.cs
namespace Dynamite.Application.Services;

using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class ModerationService : IModerationService
{
    private readonly IModerationRepository _moderationRepo;
    private readonly IWarningRepository _warningRepo;
    private readonly IGuildConfigRepository _guildConfigRepo;
    private readonly ILogger<ModerationService> _logger;

    public ModerationService(
        IModerationRepository moderationRepo,
        IWarningRepository warningRepo,
        IGuildConfigRepository guildConfigRepo,
        ILogger<ModerationService> logger)
    {
        _moderationRepo = moderationRepo;
        _warningRepo = warningRepo;
        _guildConfigRepo = guildConfigRepo;
        _logger = logger;
    }

    public async Task<ModerationAction> WarnAsync(
        ulong guildId, string guildName, ulong targetId, ulong moderatorId,
        string reason, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);

        var warning = new Warning
        {
            GuildId = guildId,
            TargetUserId = targetId,
            ModeratorId = moderatorId,
            Reason = reason,
            GuildConfigId = config.Id
        };
        await _warningRepo.AddAsync(warning, ct);
        await _warningRepo.SaveChangesAsync(ct);

        var action = await LogActionAsync(guildId, targetId, moderatorId,
            ModerationActionType.Warn, reason, config.Id, ct: ct);

        _logger.LogInformation("User {TargetId} warned in guild {GuildId} by {ModId}",
            targetId, guildId, moderatorId);

        return action;
    }

    public async Task<ModerationAction> KickAsync(
        ulong guildId, string guildName, ulong targetId, ulong moderatorId,
        string reason, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        var action = await LogActionAsync(guildId, targetId, moderatorId,
            ModerationActionType.Kick, reason, config.Id, ct: ct);

        _logger.LogInformation("User {TargetId} kicked from guild {GuildId} by {ModId}",
            targetId, guildId, moderatorId);

        return action;
    }

    public async Task<ModerationAction> BanAsync(
        ulong guildId, string guildName, ulong targetId, ulong moderatorId,
        string reason, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        var action = await LogActionAsync(guildId, targetId, moderatorId,
            ModerationActionType.Ban, reason, config.Id, ct: ct);

        _logger.LogInformation("User {TargetId} banned from guild {GuildId} by {ModId}",
            targetId, guildId, moderatorId);

        return action;
    }

    public async Task<ModerationAction> UnbanAsync(
        ulong guildId, string guildName, ulong targetId, ulong moderatorId,
        string reason, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        var action = await LogActionAsync(guildId, targetId, moderatorId,
            ModerationActionType.Unban, reason, config.Id, ct: ct);

        _logger.LogInformation("User {TargetId} unbanned from guild {GuildId} by {ModId}",
            targetId, guildId, moderatorId);

        return action;
    }

    public async Task<ModerationAction> TimeoutAsync(
        ulong guildId, string guildName, ulong targetId, ulong moderatorId,
        string reason, TimeSpan duration, CancellationToken ct = default)
    {
        if (duration.TotalSeconds < 5 || duration.TotalDays > 28)
            throw new ArgumentException("Timeout duration must be between 5 seconds and 28 days.");

        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        var action = await LogActionAsync(guildId, targetId, moderatorId,
            ModerationActionType.Timeout, reason, config.Id,
            expiresAt: DateTime.UtcNow.Add(duration), ct: ct);

        _logger.LogInformation("User {TargetId} timed out in guild {GuildId} for {Duration}",
            targetId, guildId, duration);

        return action;
    }

    public async Task<ModerationAction> UntimeoutAsync(
        ulong guildId, string guildName, ulong targetId, ulong moderatorId,
        CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        var action = await LogActionAsync(guildId, targetId, moderatorId,
            ModerationActionType.Untimeout, "Timeout removed", config.Id, ct: ct);

        return action;
    }

    public async Task<IEnumerable<Warning>> GetWarningsAsync(
        ulong guildId, ulong userId, CancellationToken ct = default)
        => await _warningRepo.GetActiveWarningsAsync(guildId, userId, ct);

    public async Task<IEnumerable<ModerationAction>> GetHistoryAsync(
        ulong guildId, ulong userId, CancellationToken ct = default)
        => await _moderationRepo.GetUserHistoryAsync(guildId, userId, ct);

    /// <summary>
    /// Soft-delete a warning (IsActive = false) scoped to a guild.
    /// Throws KeyNotFoundException if the warning doesn't exist in this guild.
    /// We soft-delete rather than hard-delete to preserve audit trail integrity.
    /// </summary>
    public async Task DeleteWarningAsync(
        ulong guildId, Guid warningId, CancellationToken ct = default)
    {
        var warning = await _warningRepo.GetByGuildAndIdAsync(guildId, warningId, ct);

        if (warning is null)
            throw new KeyNotFoundException($"Warning {warningId} not found in guild {guildId}.");

        // Soft delete: keep the record for audit trail, just mark inactive
        warning.IsActive = false;
        warning.UpdatedAt = DateTime.UtcNow;

        await _warningRepo.UpdateAsync(warning, ct);
        await _warningRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Warning {WarningId} deleted in guild {GuildId}", warningId, guildId);
    }

    private async Task<ModerationAction> LogActionAsync(
        ulong guildId, ulong targetId, ulong moderatorId,
        ModerationActionType actionType, string reason, Guid guildConfigId,
        DateTime? expiresAt = null, CancellationToken ct = default)
    {
        var action = new ModerationAction
        {
            GuildId = guildId,
            TargetUserId = targetId,
            ModeratorId = moderatorId,
            ActionType = actionType,
            Reason = reason,
            ExpiresAt = expiresAt,
            GuildConfigId = guildConfigId
        };

        await _moderationRepo.AddAsync(action, ct);
        await _moderationRepo.SaveChangesAsync(ct);
        return action;
    }
}