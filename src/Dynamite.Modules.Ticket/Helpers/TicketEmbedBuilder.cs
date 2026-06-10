// src/Dynamite.Modules.Ticket/Helpers/TicketEmbedBuilder.cs
namespace Dynamite.Modules.Ticket.Helpers;

using Discord;

public static class TicketEmbedBuilder
{
    public const string OpenButtonId  = "ticket:open";
    public const string CloseButtonId = "ticket:close";
    public const string DeleteButtonId = "ticket:delete";

    public static Embed BuildPanelEmbed(string? description = null)
    {
        return new EmbedBuilder()
            .WithTitle("🎫 Support Tickets")
            .WithDescription(
                description ??
                "Click the button below to open a support ticket.\nOur staff will assist you shortly.")
            .WithColor(new Color(0x5865F2))
            .WithFooter("One ticket per user at a time")
            .Build();
    }

    public static Embed BuildOpenedEmbed(ulong ownerId, int ticketNumber, string? topic)
    {
        return new EmbedBuilder()
            .WithTitle($"🎫 Ticket #{ticketNumber:D4}")
            .WithDescription(
                $"Welcome <@{ownerId}>!\n\n" +
                (topic != null ? $"**Topic:** {topic}\n\n" : "") +
                "Please describe your issue and a staff member will assist you shortly.\n" +
                "Click 🔒 **Close** when your issue is resolved.")
            .WithColor(new Color(0x57F287))
            .WithFooter($"Ticket opened by {ownerId}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public static Embed BuildClosedEmbed(ulong closedById, int ticketNumber)
    {
        return new EmbedBuilder()
            .WithTitle($"🔒 Ticket #{ticketNumber:D4} Closed")
            .WithDescription(
                $"This ticket was closed by <@{closedById}>.\n\n" +
                "Staff can delete this ticket using the button below.")
            .WithColor(new Color(0xED4245))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public static MessageComponent BuildPanelComponents()
        => new ComponentBuilder()
            .WithButton("Open Ticket", OpenButtonId, ButtonStyle.Primary, Emoji.Parse("🎫"))
            .Build();

    public static MessageComponent BuildOpenedComponents()
        => new ComponentBuilder()
            .WithButton("Close Ticket", CloseButtonId, ButtonStyle.Danger, Emoji.Parse("🔒"))
            .Build();

    public static MessageComponent BuildClosedComponents()
        => new ComponentBuilder()
            .WithButton("Delete Ticket", DeleteButtonId, ButtonStyle.Secondary, Emoji.Parse("🗑️"))
            .Build();
}