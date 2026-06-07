// src/Dynamite.API/Services/GuildPresenceService.cs
namespace Dynamite.API.Services;

using Dynamite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// API-side service để check BotPresent.
/// Không inject IGuildPresenceRepository vì API project không reference
/// Dynamite.Core.Interfaces.Repositories trực tiếp — dùng AppDbContext thẳng
/// để giữ API layer nhẹ, không phát sinh dependency chain không cần thiết.
/// </summary>
public class GuildPresenceService
{
    private readonly AppDbContext _db;

    public GuildPresenceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsBotPresentAsync(ulong guildId, CancellationToken ct = default)
    {
        return await _db.GuildPresences
            .AnyAsync(g => g.GuildId == guildId && g.IsPresent, ct);
    }

    public async Task<HashSet<string>> GetPresentGuildIdsAsync(
        IEnumerable<string> guildIds,
        CancellationToken ct = default)
    {
        // Batch check — tránh N+1 query khi lấy danh sách guilds
        var ulongIds = guildIds
            .Where(id => ulong.TryParse(id, out _))
            .Select(ulong.Parse)
            .ToList();

        var presentIds = await _db.GuildPresences
            .Where(g => ulongIds.Contains(g.GuildId) && g.IsPresent)
            .Select(g => g.GuildId)
            .ToListAsync(ct);

        return presentIds.Select(id => id.ToString()).ToHashSet();
    }
}