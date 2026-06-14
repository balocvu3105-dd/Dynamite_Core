// src/Dynamite.Modules.Voice/Helpers/TempVoiceEmbeds.cs
namespace Dynamite.Modules.Voice.Helpers;

using Discord;

public static class TempVoiceEmbeds
{
    private static readonly Color VoiceColor = new(0x5865F2); // Discord blurple

    public static Embed SetupSuccess(ulong triggerChannelId, ulong? categoryId) =>
        new EmbedBuilder()
            .WithTitle("🎙️ Temp Voice — Configured")
            .WithColor(Color.Green)
            .WithDescription($"Users who join <#{triggerChannelId}> will get their own private voice room.")
            .AddField("Trigger Channel", $"<#{triggerChannelId}>", inline: true)
            .AddField("Category", categoryId.HasValue ? $"<#{categoryId}>" : "Same as trigger", inline: true)
            .WithFooter("Room is auto-deleted when the owner leaves.")
            .Build();

    public static Embed Disabled() =>
        new EmbedBuilder()
            .WithTitle("🎙️ Temp Voice — Disabled")
            .WithColor(Color.Orange)
            .WithDescription("Temp Voice has been disabled for this server.")
            .Build();

    public static Embed NotConfigured() =>
        new EmbedBuilder()
            .WithTitle("❌ Not Configured")
            .WithColor(Color.Red)
            .WithDescription("Temp Voice is not set up yet. Use `/tempvoice setup` first.")
            .Build();

    public static Embed RoomCreated(string ownerName, int userLimit) =>
        new EmbedBuilder()
            .WithTitle("🔊 Your room is ready!")
            .WithColor(VoiceColor)
            .WithDescription($"**{ownerName}** owns this room and has full control.")
            .AddField("User Limit", userLimit == 0 ? "Unlimited" : userLimit.ToString(), inline: true)
            .WithFooter("Room will be deleted when you leave.")
            .Build();
}
