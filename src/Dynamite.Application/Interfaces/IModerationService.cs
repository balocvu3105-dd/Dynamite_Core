// src/Dynamite.Application/Interfaces/IModerationService.cs
namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;

public interface IModerationService
{
    Task<ModerationAction> WarnAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, CancellationToken ct = default);
    Task<ModerationAction> KickAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, CancellationToken ct = default);
    Task<ModerationAction> BanAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, CancellationToken ct = default);
    Task<ModerationAction> UnbanAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, CancellationToken ct = default);
    Task<ModerationAction> TimeoutAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, string reason, TimeSpan duration, CancellationToken ct = default);
    Task<ModerationAction> UntimeoutAsync(ulong guildId, string guildName, ulong targetId, ulong moderatorId, CancellationToken ct = default);
    Task<IEnumerable<Warning>> GetWarningsAsync(ulong guildId, ulong userId, CancellationToken ct = default);
    Task<IEnumerable<ModerationAction>> GetHistoryAsync(ulong guildId, ulong userId, CancellationToken ct = default);

    /// <summary>
    /// Delete a warning by ID, scoped to a guild.
    /// Throws KeyNotFoundException if not found.
    /// </summary>
    Task DeleteWarningAsync(ulong guildId, Guid warningId, CancellationToken ct = default);
}