// src/Dynamite.Core/Interfaces/Repositories/ISpecialPoolRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface ISpecialPoolRepository
{
    Task<List<SpecialPool>> GetActivePoolsAsync(ulong guildId);
    Task<SpecialPool?> GetByIdAsync(Guid poolId);
    Task AddPoolAsync(SpecialPool pool);

    /// <summary>Trả về số pool đã spawn trong ngày UTC hôm nay cho guild.</summary>
    Task<int> GetTodayPoolCountAsync(ulong guildId, DateTime utcDate);

    /// <summary>Số ngọc câu được trong guild trong khoảng since đến nay.</summary>
    Task<int> GetGuildPearlCountAsync(ulong guildId, DateTime since);

    /// <summary>Ghi log mỗi khi ngọc quý được câu.</summary>
    Task AddPearlLogAsync(GuildPearlLog log);

    Task SaveChangesAsync();
}
