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

    [SlashCommand("set-embed", "Customize the welcome embed (title, color, footer)")]
    public async Task SetEmbedAsync()
    {
        // Modal handler nằm ở WelcomeEmbedModalModule (ngoài [Group] —
        // Discord.Net prefix custom_id của mọi interaction trong group)
        await RespondWithModalAsync<WelcomeEmbedModal>("welcome_embed_style");
    }

    [SlashCommand("toggle-image", "Enable or disable the welcome image")]
    public async Task ToggleImageAsync(
        [Summary("enabled", "Show the generated welcome image?")] bool enabled)
    {
        await DeferAsync(ephemeral: true);
        await _welcomeService.SetImageEnabledAsync(
            Context.Guild.Id, Context.Guild.Name, enabled);

        await FollowupAsync(embed: WelcomeEmbeds.Success(
            $"Welcome Image {(enabled ? "Enabled" : "Disabled")}",
            $"The welcome image is now **{(enabled ? "on" : "off")}**."), ephemeral: true);
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
            .WithColor(WelcomeEmbeds.ParseColor(config?.WelcomeEmbedColor) ?? new Color(0x57F287))
            .AddField("Status", statusStr, inline: true)
            .AddField("Channel", channelStr, inline: true)
            .AddField("Image", config?.WelcomeImageEnabled != false ? "✅ On" : "❌ Off", inline: true)
            .AddField("Message", messageStr)
            .AddField("Embed Title", config?.WelcomeEmbedTitle ?? "*default*", inline: true)
            .AddField("Embed Color", config?.WelcomeEmbedColor ?? "*default*", inline: true)
            .AddField("Embed Footer", config?.WelcomeEmbedFooter ?? "*default*", inline: true)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }
}

// Modal handler PHẢI nằm ngoài [Group] để match đúng custom_id
[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
public class WelcomeEmbedModalModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IWelcomeService _welcomeService;

    public WelcomeEmbedModalModule(IWelcomeService welcomeService)
    {
        _welcomeService = welcomeService;
    }

    [ModalInteraction("welcome_embed_style")]
    public async Task OnWelcomeEmbedModalAsync(WelcomeEmbedModal modal)
    {
        await DeferAsync(ephemeral: true);

        // Validate màu nếu có nhập
        if (!string.IsNullOrWhiteSpace(modal.ColorHex)
            && WelcomeEmbeds.ParseColor(modal.ColorHex) is null)
        {
            await FollowupAsync(embed: WelcomeEmbeds.Error(
                "Invalid Color",
                "Color must be a hex code like `#57F287`."), ephemeral: true);
            return;
        }

        await _welcomeService.SetEmbedStyleAsync(
            Context.Guild.Id, Context.Guild.Name,
            modal.EmbedTitle, modal.ColorHex, modal.Footer);

        await FollowupAsync(embed: WelcomeEmbeds.Success(
            "Welcome Embed Updated",
            "Embed style saved. Empty fields reset to default.\n" +
            "Placeholders: `{user}`, `{server}`, `{count}`"), ephemeral: true);
    }
}

// Modal tùy biến embed — mọi field đều optional, bỏ trống = về mặc định
public class WelcomeEmbedModal : IModal
{
    public string Title => "Customize Welcome Embed";

    [InputLabel("Embed Title (optional)")]
    [RequiredInput(false)]
    [ModalTextInput("we_title", TextInputStyle.Short,
        placeholder: "Chào mừng {user} đến với {server}!", maxLength: 256)]
    public string? EmbedTitle { get; set; }

    [InputLabel("Embed Color hex (optional)")]
    [RequiredInput(false)]
    [ModalTextInput("we_color", TextInputStyle.Short,
        placeholder: "#57F287", maxLength: 16)]
    public string? ColorHex { get; set; }

    [InputLabel("Footer (optional)")]
    [RequiredInput(false)]
    [ModalTextInput("we_footer", TextInputStyle.Short,
        placeholder: "Thành viên thứ {count}", maxLength: 256)]
    public string? Footer { get; set; }
}