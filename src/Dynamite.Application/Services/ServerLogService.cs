// src/Dynamite.Application/Services/ServerLogService.cs
namespace Dynamite.Application.Services;
using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
public class ServerLogService : IServerLogService
{
    private readonly IGuildConfigRepository _guildConfigRepo;
    private readonly IServerActivityLogRepository _activityLogRepo;
    private readonly ILogger<ServerLogService> _logger;

    public ServerLogService(
        IGuildConfigRepository guildConfigRepo,
        IServerActivityLogRepository activityLogRepo,
        ILogger<ServerLogService> logger)
    {
        _guildConfigRepo = guildConfigRepo;
        _activityLogRepo = activityLogRepo;
        _logger = logger;
    }

    public async Task<ulong?> GetLogChannelAsync(
        ulong guildId, LogCategory category, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetByGuildIdAsync(guildId, ct);
        if (config is null) return null;
        return category switch
        {
            LogCategory.Message => config.MessageLogChannelId,
            LogCategory.Member  => config.MemberLogChannelId,
            LogCategory.Voice   => config.VoiceLogChannelId,
            LogCategory.Server  => config.ServerLogChannelId,
            LogCategory.Audit   => config.AuditLogChannelId,
            LogCategory.Moderation => config.ModLogChannelId ?? config.AuditLogChannelId,
            LogCategory.Security => config.AuditLogChannelId,
            LogCategory.Economy => config.InvoiceChannelId ?? config.AuditLogChannelId,
            _ => null
        };
    }

    public async Task SetLogChannelAsync(
        ulong guildId, string guildName, LogCategory category,
        ulong channelId, CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        ApplyChannel(config, category, channelId);
        config.UpdatedAt = DateTime.UtcNow;
        await _guildConfigRepo.UpdateAsync(config, ct);
        await _guildConfigRepo.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Log channel for {Category} set to {ChannelId} in guild {GuildId}",
            category, channelId, guildId);
    }

    public async Task ClearLogChannelAsync(
        ulong guildId, string guildName, LogCategory category,
        CancellationToken ct = default)
    {
        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);
        ApplyChannel(config, category, null);
        config.UpdatedAt = DateTime.UtcNow;
        await _guildConfigRepo.UpdateAsync(config, ct);
        await _guildConfigRepo.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Log channel for {Category} cleared in guild {GuildId}", category, guildId);
    }

    public async Task LogActivityAsync(
        ulong guildId,
        LogCategory category,
        string eventType,
        string title,
        string description,
        string? actorId = null,
        string? actorUsername = null,
        string? actorAvatarUrl = null,
        string? targetId = null,
        string? targetUsername = null,
        string? metadata = null,
        CancellationToken ct = default)
    {
        try
        {
            var log = new ServerActivityLog
            {
                GuildId = guildId,
                Category = category,
                EventType = eventType,
                Title = title,
                Description = description,
                ActorId = actorId,
                ActorUsername = actorUsername,
                ActorAvatarUrl = actorAvatarUrl,
                TargetId = targetId,
                TargetUsername = targetUsername,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };
            await _activityLogRepo.AddAsync(log, ct);
            await _activityLogRepo.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save server activity log for guild {GuildId}, event {EventType}", guildId, eventType);
        }
    }

    public async Task<(IEnumerable<ServerActivityLog> Logs, int TotalCount)> GetActivityLogsAsync(
        ulong guildId,
        LogCategory? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        return await _activityLogRepo.GetLogsAsync(guildId, category, search, page, pageSize, ct);
    }

    private static void ApplyChannel(GuildConfig config, LogCategory category, ulong? channelId)
    {
        switch (category)
        {
            case LogCategory.Message: config.MessageLogChannelId = channelId; break;
            case LogCategory.Member:  config.MemberLogChannelId  = channelId; break;
            case LogCategory.Voice:   config.VoiceLogChannelId   = channelId; break;
            case LogCategory.Server:  config.ServerLogChannelId  = channelId; break;
            case LogCategory.Audit:   config.AuditLogChannelId   = channelId; break;
            case LogCategory.Moderation: config.ModLogChannelId = channelId; break;
        }
    }
}