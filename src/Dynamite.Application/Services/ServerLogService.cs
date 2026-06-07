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
    private readonly ILogger<ServerLogService> _logger;

    public ServerLogService(
        IGuildConfigRepository guildConfigRepo,
        ILogger<ServerLogService> logger)
    {
        _guildConfigRepo = guildConfigRepo;
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
            LogCategory.Member => config.MemberLogChannelId,
            LogCategory.Voice => config.VoiceLogChannelId,
            LogCategory.Server => config.ServerLogChannelId,
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

    private static void ApplyChannel(GuildConfig config, LogCategory category, ulong? channelId)
    {
        switch (category)
        {
            case LogCategory.Message: config.MessageLogChannelId = channelId; break;
            case LogCategory.Member: config.MemberLogChannelId = channelId; break;
            case LogCategory.Voice: config.VoiceLogChannelId = channelId; break;
            case LogCategory.Server: config.ServerLogChannelId = channelId; break;
        }
    }
}