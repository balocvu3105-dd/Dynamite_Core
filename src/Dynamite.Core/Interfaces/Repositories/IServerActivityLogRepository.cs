// src/Dynamite.Core/Interfaces/Repositories/IServerActivityLogRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Enums;

public interface IServerActivityLogRepository : IRepository<ServerActivityLog>
{
    Task<(IEnumerable<ServerActivityLog> Logs, int TotalCount)> GetLogsAsync(
        ulong guildId,
        LogCategory? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);
}
