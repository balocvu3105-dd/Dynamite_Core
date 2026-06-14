// src/Dynamite.Modules.Voice/TempVoiceEventHandler.cs
namespace Dynamite.Modules.Voice;

using Discord.WebSocket;
using Dynamite.Modules.Voice.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Lắng nghe UserVoiceStateUpdated để điều phối vòng đời temp rooms.
///
/// Logic:
///  - User JOIN trigger channel → tạo room riêng, move vào
///  - User LEAVE bất kỳ channel → nếu là temp room và empty → xóa
/// </summary>
public class TempVoiceEventHandler
{
    private readonly DiscordSocketClient _client;
    private readonly TempVoiceService _tempVoiceService;
    private readonly ILogger<TempVoiceEventHandler> _logger;

    public TempVoiceEventHandler(
        DiscordSocketClient client,
        TempVoiceService tempVoiceService,
        ILogger<TempVoiceEventHandler> logger)
    {
        _client = client;
        _tempVoiceService = tempVoiceService;
        _logger = logger;
    }

    public void Subscribe()
    {
        _client.UserVoiceStateUpdated += (user, before, after) =>
            SafeRun(() => OnVoiceStateUpdatedAsync(user, before, after));
        _logger.LogInformation("TempVoiceEventHandler subscribed");
    }

    private async Task OnVoiceStateUpdatedAsync(
        SocketUser user,
        SocketVoiceState before,
        SocketVoiceState after)
    {
        // Chỉ xử lý guild members
        if (user is not SocketGuildUser guildUser) return;

        var joinedChannel = after.VoiceChannel;
        var leftChannel = before.VoiceChannel;

        // ── User JOIN một channel ────────────────────────────────────────────
        if (joinedChannel is not null && joinedChannel.Id != leftChannel?.Id)
        {
            var config = await _tempVoiceService.GetConfigIfTriggerAsync(
                joinedChannel.Id, guildUser.Guild.Id);

            if (config is not null)
            {
                _logger.LogInformation("User {UserId} joined trigger channel in guild {GuildId}",
                    user.Id, guildUser.Guild.Id);
                await _tempVoiceService.HandleUserJoinedTriggerAsync(guildUser, config);
            }
        }

        // ── User LEAVE một channel ───────────────────────────────────────────
        if (leftChannel is not null && leftChannel.Id != joinedChannel?.Id)
        {
            if (_tempVoiceService.IsActiveRoom(leftChannel.Id))
            {
                await _tempVoiceService.HandleUserLeftChannelAsync(leftChannel);
            }
        }
    }

    private Task SafeRun(Func<Task> handler)
    {
        _ = Task.Run(async () =>
        {
            try { await handler(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in TempVoiceEventHandler");
            }
        });
        return Task.CompletedTask;
    }
}
