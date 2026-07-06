// src/Dynamite.API/Controllers/ModulesController.cs
namespace Dynamite.API.Controllers;

using Dynamite.API.DTOs.Guild;
using Dynamite.API.Filters;
using Dynamite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/guilds/{guildId}/modules")]
[Authorize]
[RequireGuildAdmin]
public class ModulesController : ControllerBase
{
    private readonly IGuildConfigService _guildConfig;

    public ModulesController(IGuildConfigService guildConfig)
    {
        _guildConfig = guildConfig;
    }

    /// <summary>
    /// GET /api/guilds/{guildId}/modules
    /// Trả về trạng thái enable/disable của tất cả modules.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetModules(string guildId)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var config = await _guildConfig.GetOrCreateConfigAsync(guildIdUlong, string.Empty);

        var modules = new List<ModuleStatusDto>
        {
            new("moderation", config.ModerationEnabled),
            new("welcome",    config.WelcomeEnabled),
            new("logging",    config.LoggingEnabled),
            new("autorole",   config.AutoRoleEnabled),
        };

        return Ok(modules);
    }

    /// <summary>
    /// PATCH /api/guilds/{guildId}/modules/{moduleName}
    /// Toggle một module cụ thể.
    /// </summary>
    [HttpPatch("{moduleName}")]
    public async Task<IActionResult> UpdateModule(
        string guildId,
        string moduleName,
        [FromBody] UpdateModuleRequest request)
    {
        if (!ulong.TryParse(guildId, out var guildIdUlong))
            return BadRequest(new { error = "Invalid guild ID." });

        var config = await _guildConfig.GetOrCreateConfigAsync(guildIdUlong, string.Empty);

        switch (moduleName.ToLowerInvariant())
        {
            case "moderation": config.ModerationEnabled = request.Enabled; break;
            case "welcome": config.WelcomeEnabled = request.Enabled; break;
            case "logging": config.LoggingEnabled = request.Enabled; break;
            case "autorole": config.AutoRoleEnabled = request.Enabled; break;
            default:
                return BadRequest(new { error = $"Unknown module: {moduleName}" });
        }

        await _guildConfig.UpdateConfigAsync(config);

        return Ok(new ModuleStatusDto(moduleName, request.Enabled));
    }
}