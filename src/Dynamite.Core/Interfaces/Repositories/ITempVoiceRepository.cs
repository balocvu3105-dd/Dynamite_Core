// src/Dynamite.Core/Interfaces/Repositories/ITempVoiceRepository.cs
namespace Dynamite.Core.Interfaces.Repositories;

using Dynamite.Core.Entities;

public interface ITempVoiceRepository : IRepository<TempVoiceConfig>
{
    Task<TempVoiceConfig?> GetByGuildIdAsync(ulong guildId, CancellationToken ct = default);
    Task<TempVoiceConfig?> GetByTriggerChannelAsync(ulong channelId, CancellationToken ct = default);
}
