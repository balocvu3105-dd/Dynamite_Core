// src/Dynamite.API/DTOs/Security/SecurityDto.cs
namespace Dynamite.API.DTOs.Security;

/// <summary>
/// Current anti-spam / security configuration for a guild.
/// </summary>
public record SecurityConfigDto(
    bool Enabled,
    int MessageThreshold,
    int MessageWindowSeconds,
    int MentionThreshold,
    bool AntiInvite,
    bool AntiScamLink,
    bool AntiRaid,
    int RaidThreshold
);

/// <summary>
/// Request body for PATCH /security.
/// All fields nullable — only provided fields are updated.
/// </summary>
public record UpdateSecurityConfigRequest(
    bool? Enabled,
    int? MessageThreshold,
    int? MessageWindowSeconds,
    int? MentionThreshold,
    bool? AntiInvite,
    bool? AntiScamLink,
    bool? AntiRaid,
    int? RaidThreshold
);