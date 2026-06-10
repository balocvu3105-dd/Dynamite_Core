// src/Dynamite.Modules.Economy/Services/FishingService.cs
namespace Dynamite.Modules.Economy.Services;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

public class FishingService
{
    private const int DefaultCooldownSeconds = 30;

    private readonly IWalletRepository _walletRepo;
    private readonly IShopRepository _shopRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FishingService> _logger;

    public FishingService(
        IWalletRepository walletRepo,
        IShopRepository shopRepo,
        IMemoryCache cache,
        ILogger<FishingService> logger)
    {
        _walletRepo = walletRepo;
        _shopRepo = shopRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(bool success, string? cooldownMessage, FishCatch? result, long totalCoins, string? rodName)>
        FishAsync(ulong guildId, ulong userId)
    {
        var cacheKey = $"fishing:{guildId}:{userId}";

        if (_cache.TryGetValue(cacheKey, out DateTime cooldownEnd))
        {
            var remaining = (cooldownEnd - DateTime.UtcNow).TotalSeconds;
            return (false, $"⏳ You need to wait **{remaining:F0}s** before fishing again.", null, 0, null);
        }

        var wallet = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var bestRod = await _shopRepo.GetBestRodAsync(wallet.Id);

        var cooldown = bestRod?.Item.CooldownSeconds ?? DefaultCooldownSeconds;
        var multiplier = bestRod?.Item.DropMultiplier ?? 1.0;
        var rodName = bestRod?.Item.Name;

        // Set cooldown
        _cache.Set(cacheKey, DateTime.UtcNow.AddSeconds(cooldown),
            TimeSpan.FromSeconds(cooldown));

        var result = FishingDropTable.Roll(multiplier);

        wallet.Coins += result.Coins;

        var tx = new Transaction
        {
            GuildId = guildId,
            ToWalletId = wallet.Id,
            Amount = result.Coins,
            Type = TransactionType.Fishing,
            Note = $"Caught {result.Name} ({result.Rarity})",
            CreatedAt = DateTime.UtcNow
        };

        await _walletRepo.AddTransactionAsync(tx);
        await _walletRepo.SaveChangesAsync();

        _logger.LogDebug("User {UserId} fished: {Fish} = {Coins} coins", userId, result.Name, result.Coins);

        return (true, null, result, wallet.Coins, rodName);
    }
}