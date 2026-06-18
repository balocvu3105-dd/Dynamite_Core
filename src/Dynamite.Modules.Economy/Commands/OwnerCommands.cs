// src/Dynamite.Modules.Economy/Commands/OwnerCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Lệnh dành riêng cho chủ server (Guild Owner).
/// Không dùng [RequireUserPermission] vì cần kiểm tra OwnerId, không phải role.
/// </summary>
[RequireContext(ContextType.Guild)]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public class OwnerCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IWalletRepository       _walletRepo;
    private readonly IUserProfileRepository  _profileRepo;
    private readonly IPondRepository         _pondRepo;
    private readonly ILogger<OwnerCommands>  _logger;

    public OwnerCommands(
        IWalletRepository      walletRepo,
        IUserProfileRepository profileRepo,
        IPondRepository        pondRepo,
        ILogger<OwnerCommands> logger)
    {
        _walletRepo  = walletRepo;
        _profileRepo = profileRepo;
        _pondRepo    = pondRepo;
        _logger      = logger;
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private async Task<bool> IsOwnerAsync()
    {
        if (Context.User.Id == Context.Guild.OwnerId) return true;
        await RespondAsync("❌ Lệnh này chỉ dành cho **Chủ Server**.", ephemeral: true);
        return false;
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

    // ── /take-coins ───────────────────────────────────────────────────────────

    [SlashCommand("take-coins", "👑 [Chủ Server] Trừ coins của một thành viên")]
    public async Task TakeCoinsAsync(
        [Summary("user",   "Người bị trừ coins")]         IUser target,
        [Summary("amount", "Số coins muốn trừ")]
        [MinValue(1)][MaxValue(10_000_000)]                long amount,
        [Summary("reason", "Lý do")]                       string? reason = null)
    {
        await DeferAsync(ephemeral: true);
        if (!await IsOwnerAsync()) return;
        if (target.IsBot) { await FollowupAsync("❌ Không thể trừ coins của bot.", ephemeral: true); return; }

        var wallet = await _walletRepo.GetOrCreateAsync(Context.Guild.Id, target.Id);
        var actualTake = Math.Min(amount, wallet.Coins); // không trừ âm
        wallet.Coins -= actualTake;

        await _walletRepo.AddTransactionAsync(new Transaction
        {
            GuildId      = Context.Guild.Id,
            FromWalletId = wallet.Id,
            Amount       = -actualTake,
            Type         = TransactionType.AdminGrant,
            Note         = $"[Owner Take] {reason ?? "Thu hồi bởi chủ server"}",
            CreatedAt    = DateTime.UtcNow
        });
        await _walletRepo.SaveChangesAsync();

        var displayName = (target as IGuildUser)?.DisplayName ?? target.Username;
        _logger.LogInformation("[Owner] {OwnerId} took {Amount} coins from {TargetId}", Context.User.Id, actualTake, target.Id);

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0xE74C3C))
                .WithTitle("👑 Trừ Coins Thành Công")
                .WithDescription($"Đã trừ **{actualTake:N0} coins** từ **{displayName}**.\nSố dư mới: **{wallet.Coins:N0} coins**")
                .AddField("Người bị trừ", $"<@{target.Id}>", inline: true)
                .AddField("Số coins", $"💸 **{actualTake:N0}**", inline: true)
                .AddField("Lý do", reason ?? "Thu hồi bởi chủ server", inline: false)
                .WithFooter($"Thực hiện bởi {Context.User.Username} (Server Owner)")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);
    }

    // ── /give-xp ──────────────────────────────────────────────────────────────

    [SlashCommand("give-xp", "👑 [Chủ Server] Tặng Fishing XP cho thành viên")]
    public async Task GiveXpAsync(
        [Summary("user",   "Người nhận XP")]               IUser target,
        [Summary("amount", "Số XP muốn tặng")]
        [MinValue(1)][MaxValue(9_999_999)]                  long amount,
        [Summary("reason", "Lý do")]                        string? reason = null)
    {
        await DeferAsync(ephemeral: true);
        if (!await IsOwnerAsync()) return;
        if (target.IsBot) { await FollowupAsync("❌ Không thể tặng XP cho bot.", ephemeral: true); return; }

        var profile = await _profileRepo.GetOrCreateFishingAsync(Context.Guild.Id, target.Id);
        var xpBefore    = profile.FishingXp;
        var levelBefore = profile.FishingLevel;

        profile.FishingXp    += amount;
        // Sync level đơn giản — tính level từ XP tổng (công thức giống XpService nếu có)
        profile.FishingLevel  = ComputeLevel(profile.FishingXp);

        await _profileRepo.SaveChangesAsync();

        var displayName = (target as IGuildUser)?.DisplayName ?? target.Username;
        _logger.LogInformation("[Owner] {OwnerId} gave {Xp} fishing XP to {TargetId}", Context.User.Id, amount, target.Id);

        var levelInfo = profile.FishingLevel > levelBefore
            ? $"\n🆙 Level {levelBefore} → **{profile.FishingLevel}**"
            : string.Empty;

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0x2ECC71))
                .WithTitle("👑 Tặng XP Thành Công")
                .WithDescription($"Đã tặng **{amount:N0} Fishing XP** cho **{displayName}**.{levelInfo}")
                .AddField("XP trước", $"{xpBefore:N0}", inline: true)
                .AddField("XP sau",   $"{profile.FishingXp:N0}", inline: true)
                .AddField("Level",    $"{profile.FishingLevel}", inline: true)
                .AddField("Lý do", reason ?? "Thưởng bởi chủ server", inline: false)
                .WithFooter($"Thực hiện bởi {Context.User.Username} (Server Owner)")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);
    }

    // ── /take-xp ──────────────────────────────────────────────────────────────

    [SlashCommand("take-xp", "👑 [Chủ Server] Trừ Fishing XP của thành viên")]
    public async Task TakeXpAsync(
        [Summary("user",   "Người bị trừ XP")]             IUser target,
        [Summary("amount", "Số XP muốn trừ")]
        [MinValue(1)][MaxValue(9_999_999)]                  long amount,
        [Summary("reason", "Lý do")]                        string? reason = null)
    {
        await DeferAsync(ephemeral: true);
        if (!await IsOwnerAsync()) return;
        if (target.IsBot) { await FollowupAsync("❌ Không thể trừ XP của bot.", ephemeral: true); return; }

        var profile = await _profileRepo.GetOrCreateFishingAsync(Context.Guild.Id, target.Id);
        var xpBefore    = profile.FishingXp;
        var levelBefore = profile.FishingLevel;

        profile.FishingXp    = Math.Max(0, profile.FishingXp - amount);
        profile.FishingLevel = ComputeLevel(profile.FishingXp);

        await _profileRepo.SaveChangesAsync();

        var displayName = (target as IGuildUser)?.DisplayName ?? target.Username;
        _logger.LogInformation("[Owner] {OwnerId} took {Xp} fishing XP from {TargetId}", Context.User.Id, amount, target.Id);

        var levelInfo = profile.FishingLevel < levelBefore
            ? $"\n⬇️ Level {levelBefore} → **{profile.FishingLevel}**"
            : string.Empty;

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0xE67E22))
                .WithTitle("👑 Trừ XP Thành Công")
                .WithDescription($"Đã trừ **{Math.Min(amount, xpBefore):N0} Fishing XP** từ **{displayName}**.{levelInfo}")
                .AddField("XP trước", $"{xpBefore:N0}", inline: true)
                .AddField("XP sau",   $"{profile.FishingXp:N0}", inline: true)
                .AddField("Level",    $"{profile.FishingLevel}", inline: true)
                .AddField("Lý do", reason ?? "Thu hồi bởi chủ server", inline: false)
                .WithFooter($"Thực hiện bởi {Context.User.Username} (Server Owner)")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);
    }

    // ── /set-fish-rate ────────────────────────────────────────────────────────

    [SlashCommand("set-fish-rate", "👑 [Chủ Server] Thay đổi tỉ lệ câu hụt / thoát toàn server")]
    public async Task SetFishRateAsync(
        [Summary("miss-rate",   "Tỉ lệ câu hụt 0.0–1.0 (default 0.15 = 15%). -1 = reset về default")] double missRate   = -1,
        [Summary("escape-rate", "Tỉ lệ cá thoát 0.0–1.0 (default 0.10 = 10%). -1 = reset về default")] double escapeRate = -1)
    {
        await DeferAsync(ephemeral: true);
        if (!await IsOwnerAsync()) return;

        if (missRate   != -1 && (missRate   < 0 || missRate   > 1)) { await FollowupAsync("❌ miss-rate phải từ 0.0 đến 1.0 (hoặc -1 để reset).", ephemeral: true); return; }
        if (escapeRate != -1 && (escapeRate < 0 || escapeRate > 1)) { await FollowupAsync("❌ escape-rate phải từ 0.0 đến 1.0 (hoặc -1 để reset).", ephemeral: true); return; }

        var pond = await _pondRepo.GetOrCreateAsync(Context.Guild.Id);

        if (missRate   == -1) pond.FishMissRateOverride   = null;
        else                  pond.FishMissRateOverride   = missRate;

        if (escapeRate == -1) pond.FishEscapeRateOverride = null;
        else                  pond.FishEscapeRateOverride = escapeRate;

        await _pondRepo.SaveChangesAsync();

        _logger.LogInformation("[Owner] {OwnerId} set fish rates: miss={Miss} escape={Escape} in guild {GuildId}",
            Context.User.Id, pond.FishMissRateOverride, pond.FishEscapeRateOverride, Context.Guild.Id);

        var missDisplay   = pond.FishMissRateOverride.HasValue   ? $"{pond.FishMissRateOverride.Value * 100:F1}%" : $"Default ({FishingDropTable.DefaultMissRate * 100:F0}%)";
        var escapeDisplay = pond.FishEscapeRateOverride.HasValue ? $"{pond.FishEscapeRateOverride.Value * 100:F1}%" : $"Default ({FishingDropTable.DefaultEscapeRate * 100:F0}%)";

        await FollowupAsync(
            embed: new EmbedBuilder()
                .WithColor(new Color(0x9B59B6))
                .WithTitle("👑 Cập Nhật Tỉ Lệ Câu Cá")
                .WithDescription("Thay đổi có hiệu lực ngay — áp dụng cho toàn bộ user trong server.")
                .AddField("🎣 Miss Rate",   missDisplay,   inline: true)
                .AddField("🐟 Escape Rate", escapeDisplay, inline: true)
                .WithFooter($"Thực hiện bởi {Context.User.Username} (Server Owner)")
                .WithCurrentTimestamp()
                .Build(),
            ephemeral: true);
    }

    // ── Level compute helper ──────────────────────────────────────────────────

    /// <summary>
    /// Tính level từ tổng XP — khớp với XpService.XpForNextLevel.
    /// XpForNextLevel(n) = 100 * n^1.8 (quadratic scale).
    /// </summary>
    private static int ComputeLevel(long totalXp)
    {
        int level = 0;
        while (totalXp >= XpService.XpForNextLevel(level + 1))
            level++;
        return level;
    }
}
