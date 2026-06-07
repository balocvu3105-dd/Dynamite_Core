// src/Dynamite.Core/Interfaces/Repositories/IGuildPresenceRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IGuildPresenceRepository
{
    Task<GuildPresence?> GetByGuildIdAsync(ulong guildId, CancellationToken ct = default);
    Task<IEnumerable<ulong>> GetPresentGuildIdsAsync(CancellationToken ct = default);
    Task UpsertAsync(ulong guildId, string guildName, string? iconHash, CancellationToken ct = default);
    Task MarkAbsentAsync(ulong guildId, CancellationToken ct = default);

    /// <summary>
    /// Full sync: mark tất cả present guilds về absent, rồi upsert lại list hiện tại.
    /// Dùng khi bot Ready — đảm bảo không có stale data.
    /// </summary>
    Task SyncAllAsync(IEnumerable<(ulong GuildId, string GuildName, string? IconHash)> guilds, CancellationToken ct = default);
}