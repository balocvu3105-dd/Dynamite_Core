// src/Dynamite.Application/Interfaces/ISetupService.cs
namespace Dynamite.Application.Interfaces;

/// <summary>
/// Orchestrates server template application.
/// The service layer does NOT know about Discord types — it returns a result
/// that the module/command layer can present to the user.
/// </summary>
public interface ISetupService
{
    /// <summary>Returns all available template names and descriptions.</summary>
    IReadOnlyList<(string Name, string Description)> GetAvailableTemplates();

    /// <summary>
    /// Checks whether a named template exists.
    /// </summary>
    bool TemplateExists(string name);
}