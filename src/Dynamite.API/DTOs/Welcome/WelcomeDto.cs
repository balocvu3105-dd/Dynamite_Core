// src/Dynamite.API/DTOs/Welcome/WelcomeDto.cs
namespace Dynamite.API.DTOs.Welcome;

/// <summary>
/// Current welcome + verification configuration for a guild.
/// </summary>
public record WelcomeConfigDto(
    bool WelcomeEnabled,
    string? WelcomeChannelId,
    string? WelcomeMessage,
    string? VerifyChannelId,
    string? VerifyRoleId
);

/// <summary>
/// Request body for PATCH /welcome.
/// All fields nullable — only provided fields are updated.
/// Send empty string "" to clear a channel/role.
/// </summary>
public record UpdateWelcomeConfigRequest(
    bool? WelcomeEnabled,
    string? WelcomeChannelId,
    string? WelcomeMessage,
    string? VerifyChannelId,
    string? VerifyRoleId
);