// src/Dynamite.Modules.Economy/Commands/OwnerCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Lệnh dành riêng cho chủ server (Guild Owner).
/// Không dùng [RequireUserPermission] vì cần kiểm tra OwnerId, không phải role.
/// </summary>
[RequireContext(ContextType.Guild)]
public class OwnerCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IWalletRepository _walletRepo;
    private readonly ILogger<OwnerCommands> _logger;

    public OwnerCommands(
        IWalletRepository walletRepo,
        ILogger<OwnerCommands> logger)
    {
        _walletRepo = walletRepo;
        _logger     = logger;
    }

    [SlashCommand("give-coins", "👑 [Chủ Server] Tặng coins cho một thành viên")]
    public async Task GiveCoinsAsync(
        [Summary("user",   "Người nhận coins")]          IUser target,
        [Summary("amount", "Số coins muốn tặng")]
        [MinValue(1)][MaxValue(10_000_000)]               long amount,
        [Summary("reason", "Lý do (hiển thị trong log)")] string? reason = null)
    {
        await DeferAsync(ephemeral: true);

        // ── Guild Owner check ────────────────────────────────────────────────
        if (Context.User.Id != Context.Guild.OwnerId)
        {
            await FollowupAsync(
                "❌ Lệnh này chỉ dành cho **Chủ Server**.",
                ephemeral: true);
            return;
        }

        if (target.IsBot)
        {
            await FollowupAsync("❌ Không thể tặng coins cho bot.", ephemeral: true);
            return;
        }

        var wallet = await _walletRepo.GetOrCreateAsync(Context.Guild.Id, target.Id);
        wallet.Coins += amount;

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId    = Context.Guild.Id,
            ToWalletId = wallet.Id,
            Amount     = amount,
            Type       = TransactionType.AdminGrant,
            Note       = $"[Owner Gift] {reason ?? "Tặng thưởng từ chủ server"}",
            CreatedAt  = DateTime.UtcNow
        });

        await _walletRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[Owner] {OwnerId} gave {Amount} coins to {TargetId} in guild {GuildId}. Reason: {Reason}",
            Context.User.Id, amount, target.Id, Context.Guild.Id, reason ?? "—");

        var displayName = (target as IGuildUser)?.DisplayName ?? target.Username;

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0xF1C40F))
                .WithTitle("👑 Tặng Coins Thành Công")
                .WithDescription(
                    $"Đã tặng **{amount:N0} coins** cho **{displayName}**.\n" +
                    $"Số dư mới: **{wallet.Coins:N0} coins**")
                .AddField("Người nhận", $"<@{target.Id}>", inline: true)
                .AddField("Số coins", $"💰 **{amount:N0}**", inline: true)
                .AddField("Lý do", reason ?? "Tặng thưởng từ chủ server", inline: false)
                .WithFooter($"Thực hiện bởi {Context.User.Username} (Server Owner)")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);
    }
}
