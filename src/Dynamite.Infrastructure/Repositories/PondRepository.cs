// src/Dynamite.Infrastructure/Repositories/PondRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class PondRepository : IPondRepository
{
    private readonly AppDbContext _db;
    public PondRepository(AppDbContext db) => _db = db;

    public async Task<GuildPond> GetOrCreateAsync(ulong guildId)
    {
        var pond = await GetAsync(guildId);
        if (pond is not null) return pond;

        pond = new GuildPond { GuildId = guildId };
        await _db.GuildPonds.AddAsync(pond);
        await _db.SaveChangesAsync();
        return pond;
    }

    public Task<GuildPond?> GetAsync(ulong guildId)
        => _db.GuildPonds.FirstOrDefaultAsync(p => p.GuildId == guildId);

    public Task<List<GuildPond>> GetAllAsync()
        => _db.GuildPonds.ToListAsync();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
