// src/Dynamite.Modules/Logging/LoggingEventHandler.cs
namespace Dynamite.Modules.Logging;

using Discord;
using Discord.WebSocket;
using Dynamite.Modules.Logging.Loggers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Singleton that subscribes to Discord gateway events and dispatches
/// to the appropriate category logger. Registered once at startup.
/// </summary>
public class LoggingEventHandler
{
    private readonly DiscordSocketClient _client;
    private readonly MessageLogger _messageLogger;
    private readonly MemberLogger _memberLogger;
    private readonly VoiceLogger _voiceLogger;
    private readonly ServerLogger _serverLogger;
    private readonly ILogger<LoggingEventHandler> _logger;

    public LoggingEventHandler(
        DiscordSocketClient client,
        MessageLogger messageLogger,
        MemberLogger memberLogger,
        VoiceLogger voiceLogger,
        ServerLogger serverLogger,
        ILogger<LoggingEventHandler> logger)
    {
        _client = client;
        _messageLogger = messageLogger;
        _memberLogger = memberLogger;
        _voiceLogger = voiceLogger;
        _serverLogger = serverLogger;
        _logger = logger;
    }

    public void Subscribe()
    {
        // Message
        _client.MessageDeleted += (msg, ch) => SafeRun(() => _messageLogger.OnMessageDeletedAsync(msg, ch));
        _client.MessagesBulkDeleted += (msgs, ch) => SafeRun(() => _messageLogger.OnMessagesBulkDeletedAsync(msgs, ch));
        _client.MessageUpdated += (before, after, ch) => SafeRun(() => _messageLogger.OnMessageUpdatedAsync(before, after, ch));

        // Member
        _client.UserJoined += user => SafeRun(() => _memberLogger.OnUserJoinedAsync(user));
        _client.UserLeft += (guild, user) => SafeRun(() => _memberLogger.OnUserLeftAsync(guild, user));
        _client.GuildMemberUpdated += (before, after) => SafeRun(() => _memberLogger.OnGuildMemberUpdatedAsync(before, after));

        // Voice
        _client.UserVoiceStateUpdated += (user, before, after) => SafeRun(() => _voiceLogger.OnUserVoiceStateUpdatedAsync(user, before, after));

        // Server
        _client.ChannelCreated += ch => SafeRun(() => _serverLogger.OnChannelCreatedAsync(ch));
        _client.ChannelDestroyed += ch => SafeRun(() => _serverLogger.OnChannelDestroyedAsync(ch));
        _client.RoleCreated += role => SafeRun(() => _serverLogger.OnRoleCreatedAsync(role));
        _client.RoleDeleted += role => SafeRun(() => _serverLogger.OnRoleDeletedAsync(role));

        _logger.LogInformation("LoggingEventHandler subscribed to all events");
    }

    /// <summary>
    /// Wraps every handler in try/catch — a crash in one logger
    /// must NEVER bubble up and kill the Discord.Net event loop.
    /// </summary>
    private Task SafeRun(Func<Task> handler)
    {
        _ = Task.Run(async () =>
        {
            try { await handler(); }
            catch (Exception ex) { _logger.LogError(ex, "Unhandled exception in logging handler"); }
        });
        return Task.CompletedTask;
    }
}