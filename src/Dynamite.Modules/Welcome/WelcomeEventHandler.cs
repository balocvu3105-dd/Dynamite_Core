// src/Dynamite.Modules/Welcome/WelcomeEventHandler.cs
namespace Dynamite.Modules.Welcome;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Welcome.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class WelcomeEventHandler
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WelcomeImageGenerator _imageGenerator;
    private readonly ILogger<WelcomeEventHandler> _logger;

    private const string DefaultMessage =
        "We're glad to have you here. Make sure to read the rules!";

    public WelcomeEventHandler(
        DiscordSocketClient client,
        IServiceScopeFactory scopeFactory,
        WelcomeImageGenerator imageGenerator,
        ILogger<WelcomeEventHandler> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _imageGenerator = imageGenerator;
        _logger = logger;
    }

    public void Subscribe()
    {
        _client.UserJoined += user => SafeRun(() => OnUserJoinedAsync(user));
        _logger.LogInformation("WelcomeEventHandler subscribed");
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        using var scope = _scopeFactory.CreateScope();
        var welcomeService = scope.ServiceProvider.GetRequiredService<IWelcomeService>();

        var config = await welcomeService.GetWelcomeConfigAsync(user.Guild.Id);

        // Skip nếu welcome chưa được bật hoặc chưa config channel
        if (config is null || !config.WelcomeEnabled || config.WelcomeChannelId is null)
            return;

        var channel = _client.GetGuild(user.Guild.Id)
            ?.GetTextChannel(config.WelcomeChannelId.Value);

        if (channel is null)
        {
            _logger.LogWarning("Welcome channel {ChannelId} not found in guild {GuildId}",
                config.WelcomeChannelId, user.Guild.Id);
            return;
        }

        var memberCount = user.Guild.MemberCount;
        var avatarUrl = user.GetAvatarUrl(ImageFormat.Png, 256)
                       ?? user.GetDefaultAvatarUrl();

        // Resolve placeholders trong message / title / footer tùy chỉnh
        string Resolve(string template) => ResolveMessage(
            template, user.DisplayName, user.Guild.Name, memberCount);

        var message = Resolve(config.WelcomeMessage ?? DefaultMessage);

        var embed = WelcomeEmbeds.WelcomeMessage(
            user.ToString() ?? user.Username,
            user.Id,
            user.Guild.Name,
            memberCount,
            message,
            avatarUrl,
            config.WelcomeEmbedTitle is not null ? Resolve(config.WelcomeEmbedTitle) : null,
            config.WelcomeEmbedColor,
            config.WelcomeEmbedFooter is not null ? Resolve(config.WelcomeEmbedFooter) : null);

        // Generate welcome image — chỉ khi được bật
        Stream? imageStream = null;
        if (config.WelcomeImageEnabled)
        {
            imageStream = await _imageGenerator.GenerateAsync(
                user.DisplayName,
                user.Guild.Name,
                memberCount,
                avatarUrl);
        }

        try
        {
            if (imageStream is not null)
            {
                await channel.SendFileAsync(
                    imageStream,
                    "welcome.png",
                    embed: embed);
                await imageStream.DisposeAsync();
            }
            else
            {
                // Fallback: gửi embed không có ảnh nếu generate thất bại
                await channel.SendMessageAsync(embed: embed);
            }

            _logger.LogInformation("Welcome message sent for {User} in guild {GuildId}",
                user.ToString(), user.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome message for {User}", user.ToString());
        }
    }

    // Hỗ trợ placeholders: {user}, {server}, {count}
    private static string ResolveMessage(
        string template, string username, string guildName, int memberCount)
        => template
            .Replace("{user}", username)
            .Replace("{server}", guildName)
            .Replace("{count}", memberCount.ToString());

    private Task SafeRun(Func<Task> handler)
    {
        _ = Task.Run(async () =>
        {
            try { await handler(); }
            catch (Exception ex) { _logger.LogError(ex, "Unhandled exception in WelcomeEventHandler"); }
        });
        return Task.CompletedTask;
    }
}