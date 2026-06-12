// src/Dynamite.Modules.Giveaway/Helpers/GiveawayEmbedBuilder.cs
namespace Dynamite.Modules.Giveaway.Helpers;

using Discord;
using Dynamite.Core.Entities;

public static class GiveawayEmbedBuilder
{
    public const string EnterButtonId = "giveaway:enter";

    public static Embed BuildActiveEmbed(Giveaway giveaway, int entryCount)
    {
        var timeLeft = giveaway.EndsAt - DateTime.UtcNow;
        var timeStr = FormatTimeLeft(timeLeft);

        var builder = new EmbedBuilder()
            .WithTitle("🎉 GIVEAWAY")
            .WithDescription(
                $"**Prize:** {giveaway.Prize}\n" +
                (giveaway.Description != null ? $"{giveaway.Description}\n\n" : "\n") +
                $"Click 🎉 below to enter!")
            .WithColor(new Color(0xF5A623))
            .AddField("⏰ Ends In", timeStr, inline: true)
            .AddField("🏆 Winners", giveaway.WinnerCount.ToString(), inline: true)
            .AddField("🎟️ Entries", entryCount.ToString(), inline: true)
            .WithFooter($"Ends at • Hosted by {giveaway.HostId}")
            .WithTimestamp(giveaway.EndsAt);

        // Hiện điều kiện tham gia ngay trên embed để member biết trước
        if (giveaway.MinJoinDays > 0)
            builder.AddField("📋 Requirement",
                $"Must be in this server for **{giveaway.MinJoinDays}+ days**", inline: false);

        return builder.Build();
    }

    public static Embed BuildEndedEmbed(Giveaway giveaway, List<string> winnerMentions)
    {
        var winnersStr = winnerMentions.Count > 0
            ? string.Join(", ", winnerMentions)
            : "No valid entries.";

        return new EmbedBuilder()
            .WithTitle("🎉 GIVEAWAY ENDED")
            .WithDescription(
                $"**Prize:** {giveaway.Prize}\n\n" +
                $"🏆 **Winner(s):** {winnersStr}")
            .WithColor(new Color(0x95a5a6))
            .AddField("🎟️ Total Entries", giveaway.Entries.Count.ToString(), inline: true)
            .WithFooter("Giveaway ended")
            .WithTimestamp(giveaway.EndsAt)
            .Build();
    }

    public static Embed BuildCancelledEmbed(Giveaway giveaway)
    {
        return new EmbedBuilder()
            .WithTitle("❌ GIVEAWAY CANCELLED")
            .WithDescription($"**Prize:** {giveaway.Prize}\n\nThis giveaway was cancelled.")
            .WithColor(Color.Red)
            .WithFooter("Giveaway cancelled")
            .WithTimestamp(DateTime.UtcNow)
            .Build();
    }

    public static ComponentBuilder BuildEnterButton(bool disabled = false)
    {
        return new ComponentBuilder()
            .WithButton(
                label: "Enter Giveaway",
                customId: EnterButtonId,
                emote: Emoji.Parse("🎉"),
                style: ButtonStyle.Primary,
                disabled: disabled);
    }

    private static string FormatTimeLeft(TimeSpan ts)
    {
        if (ts <= TimeSpan.Zero) return "Ended";
        if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1) return $"{ts.Hours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}