// src/Dynamite.Modules/Security/Helpers/SecurityEmbeds.cs
namespace Dynamite.Modules.Security.Helpers;

using Discord;

public static class SecurityEmbeds
{
    private static readonly Color WarnColor = new(0xFEE75C);
    private static readonly Color DangerColor = new(0xED4245);
    private static readonly Color SuccessColor = new(0x57F287);
    private static readonly Color InfoColor = new(0x5865F2);

    public static Embed SpamWarning(string username, int violationCount)
        => Build("⚠️ Slow Down!",
            $"**{username}**, please stop spamming. " +
            $"Further violations will result in a timeout. (Strike {violationCount}/3)",
            WarnColor);

    public static Embed MentionSpamWarning(string username)
        => Build("⚠️ Mention Spam Detected",
            $"**{username}**, please do not mass-mention users.",
            WarnColor);

    public static Embed InviteBlocked(string username)
        => Build("🚫 Invite Blocked",
            $"**{username}**, posting server invites is not allowed here.",
            DangerColor);

    public static Embed ScamLinkBlocked(string username)
        => Build("🚫 Suspicious Link Blocked",
            $"**{username}**, that link has been flagged as potentially dangerous.",
            DangerColor);

    public static Embed RaidAlert(int joinCount, int seconds)
        => new EmbedBuilder()
            .WithTitle("🚨 Raid Detected!")
            .WithDescription(
                $"**{joinCount}** users joined in the last **{seconds}** seconds.\n" +
                "New member joins have been temporarily restricted.")
            .WithColor(DangerColor)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    public static Embed Success(string title, string description)
        => Build(title, description, SuccessColor);

    public static Embed Error(string title, string description)
        => Build(title, description, DangerColor);

    public static Embed Info(string title, string description)
        => Build(title, description, InfoColor);

    public static Embed Build(string title, string description, Color color)
        => new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
}