// src/Dynamite.Modules.Economy/Commands/FishingCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord.Interactions;
using Dynamite.Core.Interfaces.Repositories;
using Dynamite.Modules.Economy.Helpers;
using Dynamite.Modules.Economy.Services;

[Group("fishing", "Câu cá và xem trạng thái bể")]
public class FishingCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly FishingService _fishing;
    private readonly PondService _pond;
    private readonly SpecialPoolService _specialPool;
    private readonly IUserProfileRepository _profileRepo;

    public FishingCommands(
        FishingService fishing,
        PondService pond,
        SpecialPoolService specialPool,
        IUserProfileRepository profileRepo)
    {
        _fishing     = fishing;
        _pond        = pond;
        _specialPool = specialPool;
        _profileRepo = profileRepo;
    }

    [SlashCommand("cast", "Thả cần câu cá!")]
    public async Task FishAsync()
    {
        await DeferAsync();

        var (success, reason, result) =
            await _fishing.FishAsync(Context.Guild.Id, Context.User.Id);

        if (!success)
        {
            await FollowupAsync(reason, ephemeral: true);
            return;
        }

        var embed = EconomyEmbedBuilder.BuildFishEmbed(result!);
        await FollowupAsync(embed: embed);
    }

    [SlashCommand("pond", "Xem trạng thái bể cá và thời tiết hiện tại")]
    public async Task PondAsync()
    {
        await DeferAsync(ephemeral: true);
        var status = await _pond.GetStatusAsync(Context.Guild.Id);
        var embed  = EconomyEmbedBuilder.BuildPondStatusEmbed(status);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("profile", "Xem hồ sơ câu cá và XP của bạn")]
    public async Task ProfileAsync()
    {
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
        await DeferAsync(ephemeral: true);
        var pools = await _specialPool.GetActivePoolsAsync(Context.Guild.Id);
        var embed = EconomyEmbedBuilder.BuildSpecialPoolListEmbed(pools);
        await FollowupAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("pool-cast", "Câu cá trong pool đặc biệt (Level 20+)")]
    public async Task PoolCastAsync(
        [Summary("pool-id", "ID của pool (dùng /fishing pools để xem)")] string poolId)
    {
        await DeferAsync();

        if (!Guid.TryParse(poolId, out var guid))
        {
            await FollowupAsync("❌ Pool ID không hợp lệ.", ephemeral: true);
            return;
        }

        var (success, reason, result) =
            await _specialPool.FishSpecialAsync(Context.Guild.Id, Context.User.Id, guid);

        if (!success)
        {
            await FollowupAsync(reason, ephemeral: true);
            return;
        }

        var pools = await _specialPool.GetActivePoolsAsync(Context.Guild.Id);
        var pool  = pools.FirstOrDefault(p => p.Id == guid);
        var embed = EconomyEmbedBuilder.BuildSpecialFishEmbed(result!, pool?.PoolName ?? "Pool Đặc Biệt");
        await FollowupAsync(embed: embed);
    }
}
