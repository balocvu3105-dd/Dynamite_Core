// src/Dynamite.Modules.Economy/Services/XpService.cs
namespace Dynamite.Modules.Economy.Services;

using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public record LevelUpResult(bool LeveledUp, int NewLevel, ulong? RoleAwarded, int VoiceMinutesAwarded = 0);

/// <summary>
/// Quản lý 2 XP pool riêng biệt:
///   - Server XP: từ chat và voice
///   - Fishing XP: từ câu cá
///
/// Anti-inflation:
///   - Scale quadratic: XP_needed = 100 * level^1.8
///   - Chat: 5–15 XP/tin, cooldown 1 phút
///   - Voice: 10 XP/5 phút khi đang trong channel
///   - Fishing XP: theo rarity (xem FishingXpTable)
/// </summary>
public class XpService
{
    // Chat XP range (sau cooldown 1 phút)
    private const int ChatXpMin = 5;
    private const int ChatXpMax = 15;
    private static readonly TimeSpan ChatCooldown = TimeSpan.FromMinutes(1);

    // Voice XP
    private const int VoiceXpPerTick = 10;
    private static readonly TimeSpan VoiceTick = TimeSpan.FromMinutes(5);

    // Fishing XP per rarity
    public static readonly IReadOnlyDictionary<string, int> FishingXpTable =
        new Dictionary<string, int>
        {
            ["Common"]    = 20,
            ["Uncommon"]  = 35,
            ["Rare"]      = 60,
            ["Legendary"] = 120,
            ["Mythic"]    = 200,
            ["Bronze"]    = 50,   // chests
            ["Gold"]      = 120,
            ["Diamond"]   = 300,
        };

    private readonly IUserProfileRepository _profileRepo;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<XpService> _logger;

    public XpService(
        IUserProfileRepository profileRepo,
        DiscordSocketClient client,
        ILogger<XpService> logger)
    {
        _profileRepo = profileRepo;
        _client = client;
        _logger = logger;
    }

    // ── Server XP (Chat) ─────────────────────────────────────────────────────

    /// <summary>
    /// Award server XP từ chat. Trả về level-up result nếu có.
    /// Có cooldown 1 phút để tránh spam.
    /// </summary>
    public async Task<LevelUpResult?> AwardChatXpAsync(ulong guildId, ulong userId)
    {
        var profile = await _profileRepo.GetOrCreateServerAsync(guildId, userId);
        var now = DateTime.UtcNow;

        // Cooldown check
        if (profile.LastMessageXpAt.HasValue &&
            (now - profile.LastMessageXpAt.Value) < ChatCooldown)
            return null;

        var xp = Random.Shared.Next(ChatXpMin, ChatXpMax + 1);
        profile.LastMessageXpAt = now;
        profile.ServerXp += xp;

        var result = await CheckLevelUpAsync(profile, LevelType.Server, guildId);
        await _profileRepo.SaveChangesAsync();
        return result;
    }

    // ── Server XP (Voice) ────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi user join voice — lưu thời điểm vào.
    /// </summary>
    public async Task OnVoiceJoinAsync(ulong guildId, ulong userId)
    {
        var profile = await _profileRepo.GetOrCreateServerAsync(guildId, userId);
        profile.VoiceJoinedAt = DateTime.UtcNow;
        await _profileRepo.SaveChangesAsync();
    }

    /// <summary>
    /// Gọi khi user rời voice — tính XP tích lũy theo số tick 5 phút đã hoàn thành.
    /// </summary>
    public async Task<LevelUpResult?> OnVoiceLeaveAsync(ulong guildId, ulong userId)
    {
        var profile = await _profileRepo.GetOrCreateServerAsync(guildId, userId);
        if (!profile.VoiceJoinedAt.HasValue) return null;

        var elapsed = DateTime.UtcNow - profile.VoiceJoinedAt.Value;
        var ticks = (int)(elapsed.TotalMinutes / VoiceTick.TotalMinutes);

        profile.VoiceJoinedAt = null;

        if (ticks <= 0)
        {
            await _profileRepo.SaveChangesAsync();
            return null;
        }

        var xp          = ticks * VoiceXpPerTick;
        var minutesAwarded = ticks * (int)VoiceTick.TotalMinutes;
        profile.ServerXp += xp;
        profile.TotalVoiceMinutes += minutesAwarded;

        var levelUp = await CheckLevelUpAsync(profile, LevelType.Server, guildId);
        await _profileRepo.SaveChangesAsync();

        _logger.LogDebug("User {UserId} left voice: +{Xp} server XP ({Ticks} ticks)", userId, xp, ticks);

        // Trả về LevelUpResult với VoiceMinutesAwarded để handler cập nhật leaderboard
        return new LevelUpResult(
            LeveledUp:           levelUp?.LeveledUp ?? false,
            NewLevel:            levelUp?.NewLevel ?? profile.ServerLevel,
            RoleAwarded:         levelUp?.RoleAwarded,
            VoiceMinutesAwarded: minutesAwarded);
    }

    // ── Fishing XP ───────────────────────────────────────────────────────────

    /// <summary>
    /// Award fishing XP sau một lần câu thành công.
    /// Nếu caller đã có profile entity (FishingService), pass vào để tránh load lại.
    /// </summary>
    public async Task<LevelUpResult?> AwardFishingXpAsync(
        ulong guildId, ulong userId, string rarity,
        UserFishingProfile? existingProfile = null)
    {
        var profile = existingProfile
            ?? await _profileRepo.GetOrCreateFishingAsync(guildId, userId);

        var xp = FishingXpTable.GetValueOrDefault(rarity, 20);
        profile.FishingXp += xp;

        var result = await CheckFishingLevelUpAsync(profile, guildId);
        // Không SaveChanges ở đây nếu caller tự quản lý (existingProfile != null)
        if (existingProfile is null)
            await _profileRepo.SaveChangesAsync();
        return result;
    }

    // ── Level calculation ─────────────────────────────────────────────────────

    /// <summary>XP cần để đạt level tiếp theo = 100 * level^1.8 (quadratic scale).</summary>
    public static long XpForNextLevel(int currentLevel)
        => (long)(100 * Math.Pow(Math.Max(currentLevel, 1), 1.8));

    private async Task<LevelUpResult?> CheckLevelUpAsync(
        UserServerProfile profile, LevelType type, ulong guildId)
    {
        var needed = XpForNextLevel(profile.ServerLevel + 1);
        if (profile.ServerXp < needed) return new LevelUpResult(false, profile.ServerLevel, null);

        profile.ServerLevel++;
        _logger.LogInformation("User {UserId} reached Server Level {Level}", profile.UserId, profile.ServerLevel);

        var role = await AssignLevelRoleAsync(guildId, profile.UserId, type, profile.ServerLevel);
        return new LevelUpResult(true, profile.ServerLevel, role);
    }

    private async Task<LevelUpResult?> CheckFishingLevelUpAsync(
        UserFishingProfile profile, ulong guildId)
    {
        var needed = XpForNextLevel(profile.FishingLevel + 1);
        if (profile.FishingXp < needed) return new LevelUpResult(false, profile.FishingLevel, null);

        profile.FishingLevel++;
        _logger.LogInformation("User {UserId} reached Fishing Level {Level}", profile.UserId, profile.FishingLevel);

        var role = await AssignLevelRoleAsync(guildId, profile.UserId, LevelType.Fishing, profile.FishingLevel);
        return new LevelUpResult(true, profile.FishingLevel, role);
    }

    /// <summary>
    /// Lấy role tương ứng với level từ GuildLevelRoles và assign cho user.
    /// Revoke các role level thấp hơn để tránh tích lũy roles.
    /// </summary>
    private async Task<ulong?> AssignLevelRoleAsync(
        ulong guildId, ulong userId, LevelType type, int newLevel)
    {
        var levelRoles = await _profileRepo.GetLevelRolesAsync(guildId, type);
        if (levelRoles.Count == 0) return null;

        var guild = _client.GetGuild(guildId);
        var member = guild?.GetUser(userId);
        if (member is null) return null;

        ulong? awardedRoleId = null;

        foreach (var lr in levelRoles)
        {
            if (lr.RequiredLevel == newLevel)
            {
                // Award role mới
                var role = guild!.GetRole(lr.RoleId);
                if (role is not null && !member.Roles.Any(r => r.Id == lr.RoleId))
                {
                    await member.AddRoleAsync(role);
                    awardedRoleId = lr.RoleId;
                }
            }
            else if (lr.RequiredLevel < newLevel)
            {
                // Revoke role level thấp hơn (replace pattern — giữ clean)
                if (member.Roles.Any(r => r.Id == lr.RoleId))
                    await member.RemoveRoleAsync(lr.RoleId);
            }
        }

        return awardedRoleId;
    }
}
