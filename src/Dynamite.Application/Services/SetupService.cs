// src/Dynamite.Application/Services/SetupService.cs
namespace Dynamite.Application.Services;

using Dynamite.Application.Interfaces;
using Dynamite.Core.Templates;
using Microsoft.Extensions.Logging;

/// <summary>
/// Why does SetupService not call Discord API directly?
///
/// The Application layer must not depend on Discord.Net.
/// All Discord API calls (create channel, create role, etc.) happen in the Module layer,
/// which has access to the Discord SocketGuild/IGuild context.
///
/// SetupService owns:
///   - Template registry
///   - Template validation
///   - (future) recording setup history in DB
///
/// The module (SetupModule) owns:
///   - Translating ServerTemplateDefinition → Discord API calls
///   - Progress reporting to the user
///   - Rate limit backoff
///   - Rollback on failure
/// </summary>
public class SetupService : ISetupService
{
    private readonly IReadOnlyDictionary<string, IServerTemplate> _templates;
    private readonly ILogger<SetupService> _logger;

    public SetupService(IEnumerable<IServerTemplate> templates, ILogger<SetupService> logger)
    {
        _templates = templates.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public IReadOnlyList<(string Name, string Description)> GetAvailableTemplates()
        => _templates.Values
            .Select(t => (t.Name, t.Description))
            .ToList();

    public bool TemplateExists(string name)
        => _templates.ContainsKey(name);

    /// <summary>
    /// Returns the template definition for use by the module layer.
    /// Internal — only the module should call this.
    /// </summary>
    internal IServerTemplate? GetTemplate(string name)
        => _templates.GetValueOrDefault(name);
}