// src/Dynamite.API/DTOs/Guild/ModuleDto.cs
namespace Dynamite.API.DTOs.Guild;

public record ModuleStatusDto(
    string Name,
    bool Enabled
);

public record UpdateModuleRequest(bool Enabled);