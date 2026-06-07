// src/Dynamite.Core/Templates/IServerTemplate.cs
namespace Dynamite.Core.Templates;

/// <summary>
/// Each template implements this interface.
/// The template only *describes* what should exist — it never touches Discord API.
/// The SetupService reads the description and executes it.
///
/// Why interface + descriptor pattern instead of putting Discord calls in the template?
/// → Testable without a live bot
/// → Templates are swappable data, not behavior
/// → SetupService owns all Discord API calls (single responsibility)
/// </summary>
public interface IServerTemplate
{
    string Name { get; }
    string Description { get; }

    ServerTemplateDefinition GetDefinition();
}