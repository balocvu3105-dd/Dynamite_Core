// src/Dynamite.Modules.Economy/Commands/FishingCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;

[RequireContext(ContextType.Guild)]
[Group("fishing", "Câu cá và xem trạng thái bể")]
public class FishingCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly FishingService        _fishing;
    private readonly PondService           _pond;
    private readonly SpecialPoolService    _specialPool;
    private readonly IUserProfileRepository _profileRepo;
    private readonly IGuildConfigRepository _configRepo;
    private readonly IShopRepository       _shopRepo;
    private readonly IWalletRepository     _walletRepo;

    public FishingCommands(
        FishingService          fishing,
        PondService             pond,
        SpecialPoolService      specialPool,
        IUserProfileRepository  profileRepo,
        IGuildConfigRepository  configRepo,
        IShopRepository         shopRepo,
        IWalletRepository       walletRepo)
    {
        _fishing     = fishing;
        _pond        = pond;
        _specialPool = specialPool;
        _profileRepo = profileRepo;
        _configRepo  = configRepo;
        _shopRepo    = shopRepo;
        _walletRepo  = walletRepo;
    }

    [SlashCommand("cast", "Thả cần câu cá!")]
    public async Task FishAsync()
    {
        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;

        await DeferAsync();

        var guildId = Context.Guild.Id;
        var userId  = Context.User.Id;

        // ── Mutual exclusion: block manual cast nếu auto đang chạy ───────────
        var profile = await _profileRepo.GetOrCreateFishingAsync(guildId, userId);
        if (profile.AutoFishExpiresAt.HasValue && profile.AutoFishExpiresAt > DateTime.UtcNow)
        {
            await FollowupAsync(
                "⚠️ **Auto câu đang chạy!** Không thể câu tay khi auto đang hoạt động.\n" +
                "Dùng `/fish-auto stop` để dừng, hoặc `/fish-auto pause` để tạm dừng rồi câu tay.",
                ephemeral: true);
            return;
        }

        var fishResult = await _fishing.FishAsync(guildId, userId);

        if (!fishResult)
        {
            await FollowupAsync(fishResult.ErrorMessage, ephemeral: true);
            return;
        }

        var embed = EconomyEmbedBuilder.BuildFishEmbed(fishResult.Value!);
        await FollowupAsync(embed: embed);
    }

    [SlashCommand("pond", "Xem trạng thái bể cá và thời tiết hiện tại")]
    public async Task PondAsync()
    {
        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);
        var status = await _pond.GetStatusAsync(Context.Guild.Id);
        var embed  = EconomyEmbedBuilder.BuildPondStatusEmbed(status);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("profile", "Xem hồ sơ câu cá và XP của bạn")]
    public async Task ProfileAsync()
    {
        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);

        var guildId = Context.Guild.Id;
        var userId  = Context.User.Id;

        var fishingProfile = await _profileRepo.GetOrCreateFishingAsync(guildId, userId);
        var serverProfile  = await _profileRepo.GetOrCreateServerAsync(guildId, userId);

        // WalletService đã có GetWalletAsync — cần inject hoặc gọi riêng
        // Tạm dùng streak từ wallet, sẽ inject WalletService nếu cần
        var embed = EconomyEmbedBuilder.BuildProfileEmbed(
            username:     Context.User.Username,
            serverLevel:  serverProfile.ServerLevel,
            serverXp:     serverProfile.ServerXp,
            fishingLevel: fishingProfile.FishingLevel,
            fishingXp:    fishingProfile.FishingXp,
            totalCaught:  fishingProfile.TotalCaught,
            dailyStreak:  0); // inject WalletService để lấy streak nếu cần

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("achievements", "Xem danh sách thành tựu câu cá")]
    public async Task AchievementsAsync()
    {
        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);

        var profile = await _profileRepo.GetOrCreateFishingAsync(
            Context.Guild.Id, Context.User.Id);

        var embed = EconomyEmbedBuilder.BuildAchievementsEmbed(
            Context.User.Username,
            profile.Achievements.ToList());

        await FollowupAsync(embed: embed, ephemeral: true);
    }

    // ── Special Pool ──────────────────────────────────────────────────────────

    [SlashCommand("pools", "Xem các pool đặc biệt đang hoạt động (Level 20+)")]
    public async Task SpecialPoolsAsync()
    {
        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;
        await DeferAsync(ephemeral: true);
        var pools = await _specialPool.GetActivePoolsAsync(Context.Guild.Id);
        var embed = EconomyEmbedBuilder.BuildSpecialPoolListEmbed(pools);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("pool-cast", "Câu cá trong pool đặc biệt (Level 20+ | 1 vé = 1 lần câu)")]
    public async Task PoolCastAsync(
        [Summary("pool", "Chọn pool đặc biệt")]
        [Autocomplete(typeof(SpecialPoolAutocomplete))]
        string poolId)
    {
        if (!await FishingChannelGuard.CheckAsync(Context, _configRepo)) return;

        await DeferAsync();

        if (!Guid.TryParse(poolId, out var guid))
        {
            await FollowupAsync("❌ Pool ID không hợp lệ.", ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var userId  = Context.User.Id;

        // ── Pre-flight: kiểm tra pool + level trước khi tiêu vé ─────────────
        var pools = await _specialPool.GetActivePoolsAsync(guildId);
        var pool  = pools.FirstOrDefault(p => p.Id == guid);

        if (pool is null)
        {
            await FollowupAsync("❌ Pool này không còn hoạt động.", ephemeral: true);
            return;
        }

        var fishingProfile = await _profileRepo.GetOrCreateFishingAsync(guildId, userId);
        if (fishingProfile.FishingLevel < pool.MinLevel)
        {
            await FollowupAsync(
                $"❌ Cần **Fishing Level {pool.MinLevel}** để vào pool này. " +
                $"Bạn đang ở Level **{fishingProfile.FishingLevel}**.",
                ephemeral: true);
            return;
        }

        // ── Ticket check: 1 vé = 1 cast ──────────────────────────────────────
        var wallet     = await _walletRepo.GetOrCreateAsync(guildId, userId);
        var ticketItem = await _shopRepo.GetItemByTypeAsync(guildId, ItemType.PoolTicket);
        var ticket     = ticketItem is null ? null
                       : await _shopRepo.GetUserItemAsync(wallet.Id, ticketItem.Id);

        if (ticket is null || ticket.Quantity <= 0)
        {
            await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithColor(new Color(0xE74C3C))
                    .WithTitle("❌ Cần Vé Pool Đặc Biệt")
                    .WithDescription(
                        "Bạn cần **🎟️ Vé Pool Đặc Biệt** để câu tại pool này.\n" +
                        "**1 vé = 1 lần câu**\n\n" +
                        "Mua tại: `/shop buy Vé Pool Đặc Biệt` — **15,000 xu**")
                    .Build(),
                ephemeral: true);
            return;
        }

        // Tiêu 1 vé TRƯỚC khi fish (pre-flight đã pass — lỗi sau này là edge case hiếm)
        ticket.Quantity--;
        if (ticket.Quantity <= 0)
            await _shopRepo.RemoveUserInventoryAsync(ticket);
        await _shopRepo.SaveChangesAsync();

        // ── Fish ──────────────────────────────────────────────────────────────
        var specialResult = await _specialPool.FishSpecialAsync(guildId, userId, guid);

        if (!specialResult)
        {
            await FollowupAsync($"❌ {specialResult.ErrorMessage}", ephemeral: true);
            return;
        }

        var embed = EconomyEmbedBuilder.BuildSpecialFishEmbed(specialResult.Value!, pool.PoolName);
        await FollowupAsync(embed: embed);
    }
}
