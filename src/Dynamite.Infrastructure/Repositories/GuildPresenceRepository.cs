// src/Dynamite.Infrastructure/Repositories/GuildPresenceRepository.cs
namespace Dynamite.Infrastructure.Repositories;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class GuildPresenceRepository : IGuildPresenceRepository
{
    private readonly AppDbContext _db;

    public GuildPresenceRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<GuildPresence?> GetByGuildIdAsync(ulong guildId, CancellationToken ct = default)
        => _db.GuildPresences.FirstOrDefaultAsync(g => g.GuildId == guildId, ct);

    public async Task<IEnumerable<ulong>> GetPresentGuildIdsAsync(CancellationToken ct = default)
    {
        // Trả về raw long, convert về ulong sau
        // Vì EF lưu ulong as long, query trực tiếp về ulong list
        var list = await _db.GuildPresences
            .Where(g => g.IsPresent)
            .Select(g => g.GuildId)
            .ToListAsync(ct);
        return list;
    }

    public async Task UpsertAsync(
        ulong guildId,
        string guildName,
        string? iconHash,
        CancellationToken ct = default)
    {
        var existing = await GetByGuildIdAsync(guildId, ct);

        if (existing is null)
        {
            _db.GuildPresences.Add(new GuildPresence
            {
                GuildId = guildId,
                GuildName = guildName,
                IconHash = iconHash,
                IsPresent = true,
                LastSeenAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.GuildName = guildName;
            existing.IconHash = iconHash;
            existing.IsPresent = true;
            existing.LastSeenAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAbsentAsync(ulong guildId, CancellationToken ct = default)
    {
        var existing = await GetByGuildIdAsync(guildId, ct);
        if (existing is null) return;

        existing.IsPresent = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SyncAllAsync(
        IEnumerable<(ulong GuildId, string GuildName, string? IconHash)> guilds,
        CancellationToken ct = default)
    {
        // Bước 1: Mark tất cả về absent
        var allPresent = await _db.GuildPresences
            .Where(g => g.IsPresent)
            .ToListAsync(ct);

        foreach (var p in allPresent)
            p.IsPresent = false;

        // Bước 2: Upsert từng guild hiện tại
        var guildList = guilds.ToList();
        var existingMap = await _db.GuildPresences
            .Where(g => guildList.Select(x => x.GuildId).Contains(g.GuildId))
            .ToDictionaryAsync(g => g.GuildId, ct);

        foreach (var (guildId, guildName, iconHash) in guildList)
        {
            if (existingMap.TryGetValue(guildId, out var existing))
            {
                existing.GuildName = guildName;
                existing.IconHash = iconHash;
                existing.IsPresent = true;
                existing.LastSeenAt = DateTime.UtcNow;
            }
            else
            {
                _db.GuildPresences.Add(new GuildPresence
                {
                    GuildId = guildId,
                    GuildName = guildName,
                    IconHash = iconHash,
                    IsPresent = true,
                    LastSeenAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}