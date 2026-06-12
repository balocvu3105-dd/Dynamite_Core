// src/Dynamite.Application/Interfaces/IWelcomeService.cs
namespace Dynamite.Application.Interfaces;

using Dynamite.Core.Entities;

public interface IWelcomeService
{
    Task<GuildConfig?> GetWelcomeConfigAsync(ulong guildId, CancellationToken ct = default);

    Task SetChannelAsync(ulong guildId, string guildName,
        ulong channelId, CancellationToken ct = default);

    Task SetMessageAsync(ulong guildId, string guildName,
        string message, CancellationToken ct = default);

    Task SetEnabledAsync(ulong guildId, string guildName,
        bool enabled, CancellationToken ct = default);

    /// <summary>Tùy biến embed welcome. Giá trị null/rỗng = trả về mặc định.</summary>
    Task SetEmbedStyleAsync(ulong guildId, string guildName,
        string? title, string? colorHex, string? footer, CancellationToken ct = default);

    Task SetImageEnabledAsync(ulong guildId, string guildName,
        bool enabled, CancellationToken ct = default);

    Task SetVerifyChannelAsync(ulong guildId, string guildName,
        ulong channelId, CancellationToken ct = default);

    Task SetVerifyRoleAsync(ulong guildId, string guildName,
        ulong roleId, CancellationToken ct = default);

    Task<(ulong? roleId, bool configured)> GetVerifyRoleAsync(
        ulong guildId, CancellationToken ct = default);

    /// <summary>Set role sẽ bị thu hồi sau khi verify. Null để tắt tính năng.</summary>
    Task SetVerifyRemoveRoleAsync(ulong guildId, string guildName,
        ulong? roleId, CancellationToken ct = default);

    Task<ulong?> GetVerifyRemoveRoleAsync(ulong guildId, CancellationToken ct = default);
}