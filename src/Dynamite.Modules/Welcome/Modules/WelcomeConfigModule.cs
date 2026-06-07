// src/Dynamite.Modules/Welcome/Modules/WelcomeConfigModule.cs
namespace Dynamite.Modules.Welcome.Modules;

using Discord;
using Discord.Interactions;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Welcome.Helpers;
using Microsoft.Extensions.Logging;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
[Group("welcome", "Configure the welcome system")]
public class WelcomeConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IWelcomeService _welcomeService;
    private readonly ILogger<WelcomeConfigModule> _logger;

    public WelcomeConfigModule(IWelcomeService welcomeService, ILogger<WelcomeConfigModule> logger)
    {
        _welcomeService = welcomeService;
        _logger = logger;
    }

    [SlashCommand("set-channel", "Set the channel where welcome messages are sent")]
    public async Task SetChannelAsync(
        [Summary("channel", "The welcome channel")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);
        await _welcomeService.SetChannelAsync(
            Context.Guild.Id, Context.Guild.Name, channel.Id);

        await FollowupAsync(embed: WelcomeEmbeds.Success(
            "Welcome Channel Set",
            $"Welcome messages will be sent to {channel.Mention}."), ephemeral: true);
    }

    [SlashCommand("set-message", "Set the welcome message (supports {user}, {server})")]
    public async Task SetMessageAsync(
        [Summary("message", "The welcome message template")] string message)
    {
        if (message.Length > 500)
        {
            await RespondAsync(embed: WelcomeEmbeds.Error(
                "Too Long", "Message must be 500 characters or less."), ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        await _welcomeService.SetMessageAsync(
            Context.Guild.Id, Context.Guild.Name, message);

        await FollowupAsync(embed: WelcomeEmbeds.Success(
            "Welcome Message Set",
            $"New message: {message}\n\n" +
            "Placeholders: `{user}` = username, `{server}` = server name"),
            ephemeral: true);
    }

    [SlashCommand("toggle", "Enable or disable the welcome system")]
    public async Task ToggleAsync(
        [Summary("enabled", "Enable or disable")] bool enabled)
    {
        await DeferAsync(ephemeral: true);
        await _welcomeService.SetEnabledAsync(
            Context.Guild.Id, Context.Guild.Name, enabled);

        await FollowupAsync(embed: WelcomeEmbeds.Success(
            $"Welcome {(enabled ? "Enabled" : "Disabled")}",
            $"Welcome messages are now **{(enabled ? "on" : "off")}**."), ephemeral: true);
    }

    [SlashCommand("view", "View current welcome configuration")]
    public async Task ViewAsync()
    {
        await DeferAsync(ephemeral: true);

        var config = await _welcomeService.GetWelcomeConfigAsync(Context.Guild.Id);

        var channelStr = config?.WelcomeChannelId.HasValue == true
            ? $"<#{config.WelcomeChannelId}>"
            : "*not set*";

        var messageStr = config?.WelcomeMessage ?? "*default*";
        var statusStr = config?.WelcomeEnabled == true ? "✅ Enabled" : "❌ Disabled";

        var embed = new EmbedBuilder()
            .WithTitle("👋 Welcome Configuration")
            .WithColor(new Color(0x57F287))
            .AddField("Status", statusStr, inline: true)
            .AddField("Channel", channelStr, inline: true)
            .AddField("Message", messageStr)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }
}