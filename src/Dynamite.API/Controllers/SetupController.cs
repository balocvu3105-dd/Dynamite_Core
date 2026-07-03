// src/Dynamite.API/Controllers/SetupController.cs
namespace Dynamite.API.Controllers;

using Dynamite.Modules.Setup.Services;
using Dynamite.Modules.Setup.Templates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public record SmartSetupRequestDto(
    string Topic,
    string Scale,
    bool EnableEconomy,
    bool EnableTicket,
    bool EnableModeration,
    bool EnableVoice
);

[ApiController]
[Route("api/guilds/{guildId}/setup")]
[Authorize]
public class SetupController : ControllerBase
{
    private readonly SmartSetupEngine _smartEngine;

    public SetupController(SmartSetupEngine smartEngine)
    {
        _smartEngine = smartEngine;
    }

    [HttpGet("templates")]
    public IActionResult GetTemplates()
    {
        var templates = new[]
        {
            new { id = "gaming", name = "Gaming & Esports", description = "Gaming community server with LFG, clips, and squad voice channels." },
            new { id = "community", name = "General Community", description = "Hangout space with general discussions, memes, and lounges." },
            new { id = "streamer", name = "Streamer & Content Creator", description = "Stream alerts, fan art, live notifications, and sub lounges." }
        };

        return Ok(templates);
    }

    [HttpPost("preview")]
    public IActionResult PreviewSmartSetup([FromBody] SmartSetupRequestDto request)
    {
        var topic = Enum.TryParse<SmartServerTopic>(request.Topic, true, out var t) ? t : SmartServerTopic.Community;
        var scale = Enum.TryParse<SmartServerScale>(request.Scale, true, out var s) ? s : SmartServerScale.Medium;

        var options = new SmartSetupOptions
        {
            Topic = topic,
            Scale = scale,
            EnableEconomy = request.EnableEconomy,
            EnableTicket = request.EnableTicket,
            EnableModeration = request.EnableModeration,
            EnableVoice = request.EnableVoice
        };

        var template = _smartEngine.GenerateTemplate(options);

        var preview = new
        {
            name = template.Name,
            description = template.Description,
            roles = template.Roles.Select(r => new
            {
                name = r.Name,
                color = $"#{r.Color.RawValue:X6}",
                hoisted = r.Hoisted
            }),
            categories = template.Categories.Select(c => new
            {
                name = c.Name,
                channels = c.Channels.Select(ch => new
                {
                    name = ch.Name,
                    type = ch.Type.ToString(),
                    topic = ch.Topic
                })
            })
        };

        return Ok(preview);
    }
}
