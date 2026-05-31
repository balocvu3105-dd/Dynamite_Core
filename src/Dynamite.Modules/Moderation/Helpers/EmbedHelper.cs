namespace Dynamite.Modules.Moderation.Helpers;

using Discord;

public static class EmbedHelper
{
    private static readonly Color SuccessColor = new(0x57F287); // green
    private static readonly Color ErrorColor   = new(0xED4245); // red
    private static readonly Color WarnColor    = new(0xFEE75C); // yellow
    private static readonly Color InfoColor    = new(0x5865F2); // blurple

    public static Embed Success(string title, string description)
        => Build(title, description, SuccessColor);

    public static Embed Error(string title, string description)
        => Build(title, description, ErrorColor);

    public static Embed Warn(string title, string description)
        => Build(title, description, WarnColor);

    public static Embed Info(string title, string description)
        => Build(title, description, InfoColor);

    public static Embed ModerationAction(
        string action, string target, string moderator,
        string reason, string? extra = null)
    {
        var builder = new EmbedBuilder()
           .WithTitle($"🔨 {action}")
            .WithColor(ErrorColor)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("Target", target, inline: true)
            .AddField("Moderator", moderator, inline: true)
            .AddField("Reason", reason);

        if (extra is not null)
            builder.AddField("Details", extra);

        return builder.Build();
    }

    private static Embed Build(string title, string description, Color color)
        => new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
}
