// src/Dynamite.Modules/RoleManagement/Helpers/RolePanelBuilder.cs
namespace Dynamite.Modules.RoleManagement.Helpers;

using Discord;
using Dynamite.Core.Entities;
using Dynamite.Core.Enums;
using Dynamite.Modules.RoleManagement.Services; // thêm dòng này

// Builds the Discord MessageComponent (buttons/select menu) for a panel
// Also builds the Embed that goes with it
// Separated from the module to keep Discord UI logic isolated and testable
public class RolePanelBuilder
{
    public (Embed embed, MessageComponent component) Build(RolePanel panel)
    {
        var embed = BuildEmbed(panel);
        var component = panel.PanelType == RolePanelType.Button
            ? BuildButtons(panel)
            : BuildSelectMenu(panel);

        return (embed, component);
    }

    private static Embed BuildEmbed(RolePanel panel)
    {
        var builder = new EmbedBuilder()
            .WithTitle(panel.Title)
            .WithColor(new Color(0x5865F2)) // Discord blurple
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (panel.Description is not null)
            builder.WithDescription(panel.Description);

        return builder.Build();
    }

    private static MessageComponent BuildButtons(RolePanel panel)
    {
        var builder = new ComponentBuilder();

        // Discord limit: 5 buttons per row, max 5 rows = 25 buttons total
        foreach (var item in panel.Items.Take(25))
        {
            var emoji = ParseEmoji(item.Emoji);
            builder.WithButton(
                label: item.Label,
                customId: $"{RolePanelInteractionService.ButtonPrefix}{item.Id}",
                style: ButtonStyle.Secondary,
                emote: emoji);
        }

        return builder.Build();
    }

    private static MessageComponent BuildSelectMenu(RolePanel panel)
    {
        var options = panel.Items.Take(25).Select(item =>
        {
            var option = new SelectMenuOptionBuilder()
                .WithLabel(item.Label)
                .WithValue(item.Id.ToString());

            if (item.Description is not null)
                option.WithDescription(item.Description);

            var emoji = ParseEmoji(item.Emoji);
            if (emoji is not null)
                option.WithEmote(emoji);

            return option;
        }).ToList();

        var menu = new SelectMenuBuilder()
            .WithCustomId($"{RolePanelInteractionService.SelectPrefix}{panel.Id}")
            .WithPlaceholder("Select roles to toggle...")
            .WithMinValues(1)
            .WithMaxValues(options.Count)
            .WithOptions(options);

        return new ComponentBuilder().WithSelectMenu(menu).Build();
    }

    // Parse "🎮" (unicode) hoặc "<:name:id>" (custom emoji) hoặc null
    private static IEmote? ParseEmoji(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (Emote.TryParse(raw, out var customEmote))
            return customEmote;

        if (raw.Length <= 2)
            return new Emoji(raw);

        return null;
    }
}