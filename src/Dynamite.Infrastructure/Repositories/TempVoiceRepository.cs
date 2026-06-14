// src/Dynamite.Infrastructure/Repositories/TempVoiceRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class TempVoiceRepository : BaseRepository<TempVoiceConfig>, ITempVoiceRepository
{
    public TempVoiceRepository(AppDbContext context) : base(context) { }

    public async Task<TempVoiceConfig?> GetByGuildIdAsync(
        ulong guildId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(t => t.GuildId == guildId, ct);

    public async Task<TempVoiceConfig?> GetByTriggerChannelAsync(
        ulong channelId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(t => t.TriggerChannelId == channelId, ct);
}
