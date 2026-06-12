// src/Dynamite.Modules/Welcome/Helpers/WelcomeEmbeds.cs
namespace Dynamite.Modules.Welcome.Helpers;

using Discord;

public static class WelcomeEmbeds
{
    private static readonly Color WelcomeColor = new(0x57F287); // green
    private static readonly Color SuccessColor = new(0x57F287);
    private static readonly Color ErrorColor = new(0xED4245);
    private static readonly Color InfoColor = new(0x5865F2);

    public static Embed WelcomeMessage(
        string username, ulong userId,
        string guildName, int memberCount,
        string customMessage,
        string? avatarUrl = null,
        string? customTitle = null,
        string? customColorHex = null,
        string? customFooter = null)
    {
        var builder = new EmbedBuilder()
            .WithTitle(customTitle ?? $"Welcome to {guildName}!")
            .WithDescription(customMessage)
            .WithColor(ParseColor(customColorHex) ?? WelcomeColor)
            .AddField("Member", $"<@{userId}>", inline: true)
            .AddField("Member Count", $"#{memberCount}", inline: true)
            .WithFooter(customFooter ?? $"User ID: {userId}")
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (avatarUrl is not null)
            builder.WithThumbnailUrl(avatarUrl);

        return builder.Build();
    }

    // Parse "#57F287" hoặc "57F287" → Color. Sai format → null (dùng mặc định).
    public static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;

        var cleaned = hex.TrimStart('#');
        if (cleaned.Length != 6) return null;

        return uint.TryParse(cleaned,
            System.Globalization.NumberStyles.HexNumber, null, out var value)
            ? new Color(value)
            : null;
    }

    public static Embed VerifyPanel(string guildName)
        => new EmbedBuilder()
            .WithTitle("✅ Verification Required")
            .WithDescription(
                $"Welcome to **{guildName}**!\n\n" +
                "Click the button below to verify yourself and gain access to the server.")
            .WithColor(InfoColor)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    public static Embed Success(string title, string description)
        => Build(title, description, SuccessColor);

    public static Embed Error(string title, string description)
        => Build(title, description, ErrorColor);

    public static Embed Info(string title, string description)
        => Build(title, description, InfoColor);

    private static Embed Build(string title, string description, Color color)
        => new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
}