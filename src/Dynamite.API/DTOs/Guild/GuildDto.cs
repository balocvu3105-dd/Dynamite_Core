// src/Dynamite.API/DTOs/Guild/GuildDto.cs
namespace Dynamite.API.DTOs.Guild;

/// <summary>
/// Summary của một guild — dùng cho danh sách guilds.
/// Icon là URL đầy đủ, không phải hash.
/// ID là string vì ulong không serialize đúng trong JSON.
/// </summary>
public record GuildSummaryDto(
    string Id,
    string Name,
    string? IconUrl,
    bool BotPresent  // bot có đang trong guild này không
);

/// <summary>
/// Full settings của một guild.
/// </summary>
public record GuildSettingsDto(
    string GuildId,
    string GuildName,
    bool ModerationEnabled,
    bool WelcomeEnabled,
    bool LoggingEnabled,
    bool AutoRoleEnabled,
    string? ModLogChannelId,
    string? WelcomeChannelId,
    string? WelcomeMessage,
    string? VerifyChannelId,
    string? VerifyRoleId
);

/// <summary>
/// Request body khi PATCH guild settings.
/// Nullable — chỉ update field nào được gửi lên.
/// </summary>
public record UpdateGuildSettingsRequest(
    bool? ModerationEnabled,
    bool? WelcomeEnabled,
    bool? LoggingEnabled,
    bool? AutoRoleEnabled,
    string? ModLogChannelId,
    string? WelcomeChannelId,
    string? WelcomeMessage,
    string? VerifyChannelId,
    string? VerifyRoleId
);