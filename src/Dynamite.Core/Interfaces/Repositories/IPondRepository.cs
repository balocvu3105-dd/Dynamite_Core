// src/Dynamite.Core/Interfaces/Repositories/IPondRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface IPondRepository
{
    Task<GuildPond> GetOrCreateAsync(ulong guildId);
    Task<GuildPond?> GetAsync(ulong guildId);
    /// <summary>Trả về tất cả pond để scheduler biết guild nào cần spawn pool.</summary>
    Task<List<GuildPond>> GetAllAsync();
    Task SaveChangesAsync();
}
