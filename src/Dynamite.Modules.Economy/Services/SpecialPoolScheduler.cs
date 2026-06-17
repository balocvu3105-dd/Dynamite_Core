// src/Dynamite.Modules.Economy/Services/SpecialPoolScheduler.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// IHostedService chạy nền — spawn Special Pool theo lịch hybrid:
/// - Ngày chẵn UTC → guaranteed 1 pool
/// - Ngày lẻ UTC   → 20% none, 50% one, 30% two
///
/// Kiểm tra mỗi 5 phút, spawn vào đầu ngày UTC (00:00-00:05).
/// Pool tồn tại 2–4 tiếng (random), thông báo qua FishingChannel nếu config.
/// </summary>
public class SpecialPoolScheduler : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    // Pool types mỗi loại có tên thân thiện + enum
    private static readonly (string Name, SpecialDropTable Table)[] PoolTypes =
    [
        ("Vịnh San Hô 🪸",         SpecialDropTable.CoralBay),
        ("Đáy Đại Dương 🌊",        SpecialDropTable.DeepOcean),
        ("Rừng Ngập Mặn 🌿",        SpecialDropTable.MangroveForest),
        ("Vực Thẳm Huyền Bí 🌑",   SpecialDropTable.AbyssalZone),
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient  _discord;
    private readonly ILogger<SpecialPoolScheduler> _logger;

    public SpecialPoolScheduler(
        IServiceScopeFactory scopeFactory,
        DiscordSocketClient  discord,
        ILogger<SpecialPoolScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _discord      = discord;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SpecialPoolScheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSpawnAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SpecialPoolScheduler.CheckAndSpawnAsync");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAndSpawnAsync()
    {
        var now = DateTime.UtcNow;

        // Chỉ spawn trong cửa sổ đầu ngày (00:00–00:05 UTC)
        if (now.Hour != 0 || now.Minute >= 5)
            return;

        using var scope = _scopeFactory.CreateScope();
        var poolRepo  = scope.ServiceProvider.GetRequiredService<ISpecialPoolRepository>();
        var pondRepo  = scope.ServiceProvider.GetRequiredService<IPondRepository>();

        // Lấy tất cả guild có pond (đã tồn tại)
        var allPonds = await pondRepo.GetAllAsync();

        foreach (var pond in allPonds)
        {
            try
            {
                await SpawnForGuildAsync(pond.GuildId, now, poolRepo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to spawn pools for guild {GuildId}", pond.GuildId);
            }
        }
    }

    private async Task SpawnForGuildAsync(
        ulong guildId, DateTime now,
        ISpecialPoolRepository poolRepo)
    {
        // Tránh spawn 2 lần trong cùng ngày (check idempotency)
        var existingToday = await poolRepo.GetTodayPoolCountAsync(guildId, now);
        if (existingToday > 0) return;

        var targetCount = GetPoolCountForDay(now);
        if (targetCount == 0)
        {
            _logger.LogDebug("Guild {GuildId}: no special pool today (odd-day random)", guildId);
            return;
        }

        var selected = SelectRandomPools(targetCount);
        var pools    = new List<SpecialPool>();

        foreach (var (name, table) in selected)
        {
            var durationHours = Random.Shared.Next(2, 5); // 2–4 hours
            var pool = new SpecialPool
            {
                GuildId      = guildId,
                PoolName     = name,
                DropTable    = table,
                Capacity     = 2000,
                RemainingFish = 2000,
                MinLevel     = 20,
                StartsAt     = now,
                ExpiresAt    = now.AddHours(durationHours),
                CreatedAt    = now
            };
            await poolRepo.AddPoolAsync(pool);
            pools.Add(pool);

            _logger.LogInformation(
                "Spawned special pool [{Name}] for guild {GuildId} — expires in {Hours}h",
                name, guildId, durationHours);
        }

        await poolRepo.SaveChangesAsync();

        // Thông báo lên fishing channel nếu có
        await AnnouncePoolsAsync(guildId, pools);
    }

    // ── Schedule logic ────────────────────────────────────────────────────────

    /// <summary>
    /// Ngày chẵn UTC → guaranteed 1 pool.
    /// Ngày lẻ UTC   → 20%=0, 50%=1, 30%=2.
    /// </summary>
    private static int GetPoolCountForDay(DateTime utcNow)
    {
        if (utcNow.Day % 2 == 0)
            return 1; // Ngày chẵn: guaranteed

        // Ngày lẻ: random
        var roll = Random.Shared.NextDouble();
        return roll switch
        {
            < 0.20 => 0,
            < 0.70 => 1,
            _      => 2
        };
    }

    private static List<(string Name, SpecialDropTable Table)> SelectRandomPools(int count)
    {
        // Shuffle and take N
        var shuffled = PoolTypes
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();
        return shuffled;
    }

    // ── Discord announcement ──────────────────────────────────────────────────

    private async Task AnnouncePoolsAsync(ulong guildId, List<SpecialPool> pools)
    {
        if (pools.Count == 0) return;

        var guild = _discord.GetGuild(guildId);
        if (guild is null) return;

        // FishingChannelId lưu ở GuildPond — tạm thời dùng system channel nếu chưa config
        var channel = guild.SystemChannel;
        if (channel is null) return;

        var description = string.Join("\n", pools.Select(p =>
            $"🎣 **{p.PoolName}** — {p.RemainingFish:N0} cá | Hết lúc <t:{new DateTimeOffset(p.ExpiresAt).ToUnixTimeSeconds()}:t>"));

        var embed = new Discord.EmbedBuilder()
            .WithTitle("🌊 Pool Đặc Biệt Xuất Hiện!")
            .WithDescription(description)
            .WithColor(Discord.Color.DarkBlue)
            .WithFooter("Cần Fishing Level 20+ để tham gia • /fishing pool cast")
            .WithCurrentTimestamp()
            .Build();

        try
        {
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to announce special pools for guild {GuildId}", guildId);
        }
    }
}
