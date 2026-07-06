// src/Dynamite.API/DTOs/Logging/LoggingDto.cs
namespace Dynamite.API.DTOs.Logging;

/// <summary>
/// Current logging channel configuration for a guild.
/// All IDs are strings (ulong can't safely serialize to JSON number).
/// Null means that category is not configured.
/// </summary>
public record LoggingConfigDto(
    string? MessageLogChannelId,
    string? MemberLogChannelId,
    string? VoiceLogChannelId,
    string? ServerLogChannelId,
    string? ModLogChannelId = null,
    string? AuditLogChannelId = null
);

/// <summary>
/// Request body for PATCH /logging.
/// All fields are nullable — only provided fields are updated.
/// Send empty string "" to clear a channel.
/// </summary>
public record UpdateLoggingConfigRequest(
    string? MessageLogChannelId,
    string? MemberLogChannelId,
    string? VoiceLogChannelId,
    string? ServerLogChannelId,
    string? ModLogChannelId = null,
    string? AuditLogChannelId = null
);