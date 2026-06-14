// src/Dynamite.Modules.Voice/Services/TempVoiceService.cs
namespace Dynamite.Modules.Voice.Services;

using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Quản lý toàn bộ vòng đời của temp voice rooms.
///
/// Active rooms được giữ in-memory trong ConcurrentDictionary —
/// không cần DB vì chúng là ephemeral (tắt bot = channel đã bị xóa rồi).
/// Key = channelId của temp room, Value = ownerId.
/// </summary>
public class TempVoiceService
{
    // channelId → ownerId
    private readonly ConcurrentDictionary<ulong, ulong> _activeRooms = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TempVoiceService> _logger;

    public TempVoiceService(
        IServiceScopeFactory scopeFactory,
        ILogger<TempVoiceService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ─── Config Management ───────────────────────────────────────────────────

    public async Task<TempVoiceConfig> SetupAsync(
        ulong guildId, string guildName,
        ulong triggerChannelId,
        ulong? categoryId,
        int defaultUserLimit = 0,
        CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var tempVoiceRepo = scope.ServiceProvider.GetRequiredService<ITempVoiceRepository>();
        var guildConfigRepo = scope.ServiceProvider.GetRequiredService<IGuildConfigRepository>();

        var existing = await tempVoiceRepo.GetByGuildIdAsync(guildId, ct);
        if (existing is not null)
        {
            // Update existing config
            existing.TriggerChannelId = triggerChannelId;
            existing.CategoryId = categoryId;
            existing.DefaultUserLimit = defaultUserLimit;
            await tempVoiceRepo.SaveChangesAsync(ct);
            _logger.LogInformation("TempVoice updated for guild {GuildId}", guildId);
            return existing;
        }

        var guildConfig = await guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        var config = new TempVoiceConfig
        {
            GuildId = guildId,
            TriggerChannelId = triggerChannelId,
            CategoryId = categoryId,
            DefaultUserLimit = defaultUserLimit,
            GuildConfigId = guildConfig.Id
        };

        await tempVoiceRepo.AddAsync(config, ct);
        await tempVoiceRepo.SaveChangesAsync(ct);
        _logger.LogInformation("TempVoice configured for guild {GuildId}, trigger={ChannelId}", guildId, triggerChannelId);
        return config;
    }

    public async Task<bool> DisableAsync(ulong guildId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var tempVoiceRepo = scope.ServiceProvider.GetRequiredService<ITempVoiceRepository>();

        var config = await tempVoiceRepo.GetByGuildIdAsync(guildId, ct);
        if (config is null) return false;

        await tempVoiceRepo.DeleteAsync(config, ct);
        await tempVoiceRepo.SaveChangesAsync(ct);
        _logger.LogInformation("TempVoice disabled for guild {GuildId}", guildId);
        return true;
    }

    public async Task<TempVoiceConfig?> GetConfigAsync(ulong guildId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var tempVoiceRepo = scope.ServiceProvider.GetRequiredService<ITempVoiceRepository>();
        return await tempVoiceRepo.GetByGuildIdAsync(guildId, ct);
    }

    // ─── Room Lifecycle ──────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi user join vào trigger channel.
    /// Tạo voice channel mới, move user vào, set owner permissions.
    /// </summary>
    public async Task HandleUserJoinedTriggerAsync(SocketGuildUser user, TempVoiceConfig config)
    {
        var guild = user.Guild;

        // Resolve category: dùng config.CategoryId nếu có, fallback sang category của trigger channel
        ICategoryChannel? category = null;
        if (config.CategoryId.HasValue)
            category = guild.GetCategoryChannel(config.CategoryId.Value);

        if (category is null)
        {
            var triggerChannel = guild.GetVoiceChannel(config.TriggerChannelId);
            if (triggerChannel?.CategoryId.HasValue == true)
                category = guild.GetCategoryChannel(triggerChannel.CategoryId!.Value);
        }

        // Tạo channel với tên = display name của user
        var channelName = $"🔊 {user.DisplayName}'s Room";
        var newChannel = await guild.CreateVoiceChannelAsync(channelName, props =>
        {
            props.UserLimit = config.DefaultUserLimit == 0 ? null : config.DefaultUserLimit;
            if (category is not null)
                props.CategoryId = category.Id;
        });

        // Set owner permissions: full control trong room này
        await newChannel.AddPermissionOverwriteAsync(user, new OverwritePermissions(
            manageChannel: PermValue.Allow,
            viewChannel: PermValue.Allow,
            connect: PermValue.Allow,
            speak: PermValue.Allow,
            stream: PermValue.Allow,
            useVoiceActivation: PermValue.Allow,
            muteMembers: PermValue.Allow,
            deafenMembers: PermValue.Allow,
            moveMembers: PermValue.Allow
        ));

        // Track room
        _activeRooms[newChannel.Id] = user.Id;
        _logger.LogInformation("Temp room {ChannelId} created for owner {UserId} in guild {GuildId}",
            newChannel.Id, user.Id, guild.Id);

        // Move user vào room mới
        try
        {
            await user.ModifyAsync(props => props.Channel = newChannel);
        }
        catch (Exception ex)
        {
            // User có thể đã disconnect trong khoảng thời gian tạo channel
            _logger.LogWarning(ex, "Failed to move user {UserId} to new temp room {ChannelId}", user.Id, newChannel.Id);
            // Cleanup room nếu không move được
            _activeRooms.TryRemove(newChannel.Id, out _);
            await newChannel.DeleteAsync();
        }
    }

    /// <summary>
    /// Gọi mỗi khi user disconnect khỏi bất kỳ voice channel nào.
    /// Nếu channel đó là temp room VÀ đã trống → xóa.
    /// </summary>
    public async Task HandleUserLeftChannelAsync(SocketVoiceChannel channel)
    {
        // Không phải temp room của chúng ta → bỏ qua
        if (!_activeRooms.ContainsKey(channel.Id))
            return;

        // Còn người trong room → chưa xóa
        if (channel.ConnectedUsers.Count > 0)
        {
            _logger.LogDebug("Temp room {ChannelId} still has {Count} user(s), not deleting",
                channel.Id, channel.ConnectedUsers.Count);
            return;
        }

        // Room trống → xóa
        _activeRooms.TryRemove(channel.Id, out _);

        try
        {
            await channel.DeleteAsync();
            _logger.LogInformation("Temp room {ChannelId} deleted (empty)", channel.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp room {ChannelId}", channel.Id);
        }
    }

    /// <summary>Kiểm tra channelId có phải trigger channel của guild không.</summary>
    public async Task<TempVoiceConfig?> GetConfigIfTriggerAsync(ulong channelId, ulong guildId)
    {
        using var scope = _scopeFactory.CreateScope();
        var tempVoiceRepo = scope.ServiceProvider.GetRequiredService<ITempVoiceRepository>();
        var config = await tempVoiceRepo.GetByTriggerChannelAsync(channelId);
        return config?.GuildId == guildId ? config : null;
    }

    public bool IsActiveRoom(ulong channelId) => _activeRooms.ContainsKey(channelId);
}
