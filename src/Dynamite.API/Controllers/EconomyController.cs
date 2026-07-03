// src/Dynamite.API/Controllers/EconomyController.cs
namespace Dynamite.API.Controllers;

using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dynamite.API.Filters;

public record UpdateBalanceRequestDto(long Coins);

[ApiController]
[Route("api/guilds/{guildId}/economy")]
[Authorize]
[RequireGuildAdmin]
public class EconomyController : ControllerBase
{
    private readonly WalletService _walletService;
    private readonly IWalletRepository _walletRepo;

    public EconomyController(WalletService walletService, IWalletRepository walletRepo)
    {
        _walletService = walletService;
        _walletRepo = walletRepo;
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard(string guildId)
    {
        if (!ulong.TryParse(guildId, out var gid))
            return BadRequest(new { error = "Invalid guild ID." });

        var board = await _walletService.GetLeaderboardAsync(gid);
        var result = board.Select(b => new
        {
            rank = b.rank,
            userId = b.userId.ToString(),
            coins = b.coins
        });

        return Ok(result);
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserWallet(string guildId, string userId)
    {
        if (!ulong.TryParse(guildId, out var gid) || !ulong.TryParse(userId, out var uid))
            return BadRequest(new { error = "Invalid guild or user ID." });

        var wallet = await _walletService.GetWalletAsync(gid, uid);
        return Ok(new
        {
            userId = wallet.UserId.ToString(),
            coins = wallet.Coins,
            dailyStreak = wallet.DailyStreak,
            lastDaily = wallet.LastDaily
        });
    }

    [HttpPut("users/{userId}/balance")]
    public async Task<IActionResult> UpdateUserBalance(string guildId, string userId, [FromBody] UpdateBalanceRequestDto request)
    {
        if (!ulong.TryParse(guildId, out var gid) || !ulong.TryParse(userId, out var uid))
            return BadRequest(new { error = "Invalid guild or user ID." });

        var wallet = await _walletRepo.GetOrCreateAsync(gid, uid);
        var oldCoins = wallet.Coins;
        wallet.Coins = Math.Max(0, request.Coins);

        var diff = wallet.Coins - oldCoins;
        var tx = new Transaction
        {
            GuildId = gid,
            ToWalletId = wallet.Id,
            Amount = Math.Abs(diff),
            Type = diff >= 0 ? TransactionType.AdminGrant : TransactionType.AdminDeduct,
            Note = "Updated via Web Dashboard",
            CreatedAt = DateTime.UtcNow
        };

        await _walletRepo.AddTransactionAsync(tx);
        await _walletRepo.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            userId = userId,
            oldCoins,
            newCoins = wallet.Coins
        });
    }
}
