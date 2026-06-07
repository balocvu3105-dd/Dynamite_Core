// src/Dynamite.Modules/Logging/Helpers/LogEmbedHelper.cs
namespace Dynamite.Modules.Logging.Helpers;

using Discord;

public static class LogEmbedHelper
{
    // Consistent colors per category
    private static readonly Color MessageColor = new(0x3498DB); // blue
    private static readonly Color MemberColor = new(0x2ECC71); // green
    private static readonly Color VoiceColor = new(0x9B59B6); // purple
    private static readonly Color ServerColor = new(0xE67E22); // orange
    private static readonly Color DangerColor = new(0xED4245); // red

    // ── MESSAGE ──────────────────────────────────────────────

    public static Embed MessageDeleted(
        string authorTag, ulong authorId,
        string channelMention, string content)
        => Build("🗑️ Message Deleted", MessageColor)
            .AddField("Author", $"{authorTag} (`{authorId}`)", inline: true)
            .AddField("Channel", channelMention, inline: true)
            .AddField("Content", Truncate(content, 1024))
            .Build();

    public static Embed MessageEdited(
        string authorTag, ulong authorId,
        string channelMention, string before, string after, string jumpUrl)
        => Build("✏️ Message Edited", MessageColor)
            .AddField("Author", $"{authorTag} (`{authorId}`)", inline: true)
            .AddField("Channel", channelMention, inline: true)
            .AddField("Before", Truncate(before, 512))
            .AddField("After", Truncate(after, 512))
            .WithUrl(jumpUrl)
            .Build();

    public static Embed MessagesBulkDeleted(
        string channelMention, int count)
        => Build("🗑️ Bulk Delete", DangerColor)
            .WithDescription($"{count} messages were deleted in {channelMention}.")
            .Build();

    // ── MEMBER ───────────────────────────────────────────────

    public static Embed MemberJoined(string tag, ulong userId, DateTimeOffset accountCreated)
    {
        var age = DateTimeOffset.UtcNow - accountCreated;
        var ageStr = age.TotalDays < 7
            ? $"⚠️ {(int)age.TotalDays}d (new account!)"
            : $"{(int)age.TotalDays}d";

        return Build("✅ Member Joined", MemberColor)
            .AddField("User", $"{tag} (`{userId}`)", inline: true)
            .AddField("Account Age", ageStr, inline: true)
            .WithThumbnailUrl($"https://cdn.discordapp.com/avatars/{userId}/")
            .Build();
    }

    public static Embed MemberLeft(string tag, ulong userId)
        => Build("👋 Member Left", DangerColor)
            .AddField("User", $"{tag} (`{userId}`)", inline: true)
            .Build();

    public static Embed RoleAdded(string tag, ulong userId, string roleName)
        => Build("🎭 Role Added", MemberColor)
            .AddField("User", $"{tag} (`{userId}`)", inline: true)
            .AddField("Role", roleName, inline: true)
            .Build();

    public static Embed RoleRemoved(string tag, ulong userId, string roleName)
        => Build("🎭 Role Removed", DangerColor)
            .AddField("User", $"{tag} (`{userId}`)", inline: true)
            .AddField("Role", roleName, inline: true)
            .Build();

    public static Embed NicknameChanged(
        string tag, ulong userId, string? before, string? after)
        => Build("📝 Nickname Changed", MemberColor)
            .AddField("User", $"{tag} (`{userId}`)", inline: true)
            .AddField("Before", before ?? "*none*", inline: true)
            .AddField("After", after ?? "*none*", inline: true)
            .Build();

    // ── VOICE ────────────────────────────────────────────────

    public static Embed VoiceJoined(string tag, ulong userId, string channelName)
        => Build("🔊 Voice Joined", VoiceColor)
            .AddField("User", $"{tag} (`{userId}`)", inline: true)
            .AddField("Channel", channelName, inline: true)
            .Build();

    public static Embed VoiceLeft(string tag, ulong userId, string channelName)
        => Build("🔇 Voice Left", VoiceColor)
            .AddField("User", $"{tag} (`{userId}`)", inline: true)
            .AddField("Channel", channelName, inline: true)
            .Build();

    public static Embed VoiceMoved(
        string tag, ulong userId, string fromChannel, string toChannel)
        => Build("🔀 Voice Moved", VoiceColor)
            .AddField("User", $"{tag} (`{userId}`)", inline: true)
            .AddField("From", fromChannel, inline: true)
            .AddField("To", toChannel, inline: true)
            .Build();

    // ── SERVER ───────────────────────────────────────────────

    public static Embed ChannelCreated(string channelName, string type)
        => Build("📢 Channel Created", ServerColor)
            .AddField("Name", channelName, inline: true)
            .AddField("Type", type, inline: true)
            .Build();

    public static Embed ChannelDeleted(string channelName, string type)
        => Build("🗑️ Channel Deleted", DangerColor)
            .AddField("Name", channelName, inline: true)
            .AddField("Type", type, inline: true)
            .Build();

    public static Embed RoleCreated(string roleName)
        => Build("🎭 Role Created", ServerColor)
            .AddField("Name", roleName, inline: true)
            .Build();

    public static Embed RoleDeleted(string roleName)
        => Build("🗑️ Role Deleted", DangerColor)
            .AddField("Name", roleName, inline: true)
            .Build();

    // ── HELPERS ──────────────────────────────────────────────

    private static EmbedBuilder Build(string title, Color color)
        => new EmbedBuilder()
            .WithTitle(title)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.UtcNow);

    private static string Truncate(string text, int max)
        => string.IsNullOrEmpty(text)
            ? "*empty*"
            : text.Length <= max ? text : text[..(max - 3)] + "...";
}