// src/Dynamite.Core/Interfaces/Repositories/IBlacklistRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IBlacklistRepository : IRepository<UserBlacklist>
{
    /// <summary>Get the active blacklist entry for a specific user in a guild, or null.</summary>
    Task<UserBlacklist?> GetActiveAsync(ulong guildId, ulong userId, CancellationToken ct = default);

    /// <summary>Returns the most recent N active entries for a guild (default 50).</summary>
    Task<IEnumerable<UserBlacklist>> GetAllActiveAsync(ulong guildId, int count = 50, CancellationToken ct = default);

    /// <summary>Fast existence check — does not load the full entity.</summary>
    Task<bool> IsBlacklistedAsync(ulong guildId, ulong userId, CancellationToken ct = default);
}
