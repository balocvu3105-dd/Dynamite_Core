// src/Dynamite.Modules.Voice/Commands/TempVoiceModule.cs
namespace Dynamite.Modules.Voice.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Modules.Voice.Helpers;
using Dynamite.Modules.Voice.Services;
using Microsoft.Extensions.Logging;

[Group("tempvoice", "Temp Voice channel system")]
[DefaultMemberPermissions(GuildPermission.ManageGuild)]
public class TempVoiceModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TempVoiceService _service;
    private readonly ILogger<TempVoiceModule> _logger;

    public TempVoiceModule(TempVoiceService service, ILogger<TempVoiceModule> logger)
    {
        _service = service;
        _logger = logger;
    }

    [SlashCommand("setup", "Set the trigger voice channel for auto-room creation")]
    public async Task SetupAsync(
        [Summary("trigger", "Voice channel users join to get their own room")] IVoiceChannel trigger,
        [Summary("category", "Category to place temp rooms in (default: same as trigger)")] ICategoryChannel? category = null,
        [Summary("user-limit", "Default user limit per room (0 = unlimited)")] int userLimit = 0)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            await _service.SetupAsync(
                Context.Guild.Id,
                Context.Guild.Name,
                trigger.Id,
                category?.Id,
                userLimit);

            await FollowupAsync(embed: TempVoiceEmbeds.SetupSuccess(trigger.Id, category?.Id), ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup TempVoice for guild {GuildId}", Context.Guild.Id);
            await FollowupAsync("❌ Setup failed. Please try again.", ephemeral: true);
        }
    }

    [SlashCommand("disable", "Disable the Temp Voice system for this server")]
    public async Task DisableAsync()
    {
        await DeferAsync(ephemeral: true);

        var disabled = await _service.DisableAsync(Context.Guild.Id);
        if (!disabled)
        {
            await FollowupAsync(embed: TempVoiceEmbeds.NotConfigured(), ephemeral: true);
            return;
        }

        await FollowupAsync(embed: TempVoiceEmbeds.Disabled(), ephemeral: true);
    }

    [SlashCommand("status", "Check the current Temp Voice configuration")]
    public async Task StatusAsync()
    {
        await DeferAsync(ephemeral: true);

        var config = await _service.GetConfigAsync(Context.Guild.Id);
        if (config is null)
        {
            await FollowupAsync(embed: TempVoiceEmbeds.NotConfigured(), ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("🎙️ Temp Voice — Status")
            .WithColor(new Color(0x5865F2))
            .AddField("Trigger Channel", $"<#{config.TriggerChannelId}>", inline: true)
            .AddField("Category", config.CategoryId.HasValue ? $"<#{config.CategoryId}>" : "Same as trigger", inline: true)
            .AddField("Default User Limit", config.DefaultUserLimit == 0 ? "Unlimited" : config.DefaultUserLimit.ToString(), inline: true)
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true);
    }
}
