// src/Dynamite.Application/Services/AutoRoleService.cs
namespace Dynamite.Application.Services;

using Dynamite.Application.Interfaces;
using Dynamite.Core.Entities;
using Dynamite.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class AutoRoleService : IAutoRoleService
{
    private readonly IAutoRoleRepository _autoRoleRepo;
    private readonly IGuildConfigRepository _guildConfigRepo;
    private readonly ILogger<AutoRoleService> _logger;

    public AutoRoleService(
        IAutoRoleRepository autoRoleRepo,
        IGuildConfigRepository guildConfigRepo,
        ILogger<AutoRoleService> logger)
    {
        _autoRoleRepo = autoRoleRepo;
        _guildConfigRepo = guildConfigRepo;
        _logger = logger;
    }

    public async Task<AutoRoleConfig> AddAutoRoleAsync(
        ulong guildId, string guildName, ulong roleId, CancellationToken ct = default)
    {
        var existing = await _autoRoleRepo.GetByGuildAndRoleAsync(guildId, roleId, ct);
        if (existing is not null) return existing;

        var config = await _guildConfigRepo.GetOrCreateAsync(guildId, guildName, ct);

        var autoRole = new AutoRoleConfig
        {
            GuildId = guildId,
            RoleId = roleId,
            GuildConfigId = config.Id
        };

        await _autoRoleRepo.AddAsync(autoRole, ct);
        await _autoRoleRepo.SaveChangesAsync(ct);

        _logger.LogInformation("AutoRole {RoleId} added for guild {GuildId}", roleId, guildId);
        return autoRole;
    }

    public async Task RemoveAutoRoleAsync(
        ulong guildId, ulong roleId, CancellationToken ct = default)
    {
        var autoRole = await _autoRoleRepo.GetByGuildAndRoleAsync(guildId, roleId, ct);
        if (autoRole is null)
            throw new InvalidOperationException($"Role {roleId} is not configured as an auto role.");

        await _autoRoleRepo.DeleteAsync(autoRole, ct);
        await _autoRoleRepo.SaveChangesAsync(ct);

        _logger.LogInformation("AutoRole {RoleId} removed for guild {GuildId}", roleId, guildId);
    }

    public async Task<IEnumerable<AutoRoleConfig>> GetAutoRolesAsync(
        ulong guildId, CancellationToken ct = default)
        => await _autoRoleRepo.GetByGuildIdAsync(guildId, ct);

    public async Task<IEnumerable<ulong>> GetRoleIdsToApplyAsync(
        ulong guildId, CancellationToken ct = default)
    {
        var autoRoles = await _autoRoleRepo.GetByGuildIdAsync(guildId, ct);
        return autoRoles.Select(a => a.RoleId);
    }
}