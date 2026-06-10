// src/Dynamite.Modules.Ticket/Commands/TicketCommands.cs
namespace Dynamite.Modules.Ticket.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Modules.Ticket.Services;
using Microsoft.Extensions.Logging;

[Group("ticket", "Ticket system commands")]
public class TicketCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TicketService _service;
    private readonly ILogger<TicketCommands> _logger;

    public TicketCommands(TicketService service, ILogger<TicketCommands> logger)
    {
        _service = service;
        _logger = logger;
    }

    [SlashCommand("setup", "Set up the ticket system")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetupAsync(
        [Summary("channel", "Channel to post the ticket panel")] ITextChannel channel,
        [Summary("staff-role", "Role that can see all tickets")] IRole? staffRole = null,
        [Summary("category", "Category to create ticket channels in")] ICategoryChannel? category = null,
        [Summary("description", "Custom panel description")] string? description = null)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            var (success, message) = await _service.SetupAsync(
                Context.Guild.Id,
                channel.Id,
                staffRole?.Id,
                category?.Id,
                description);

            await FollowupAsync(success ? $"✅ {message}" : $"❌ {message}", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup ticket system");
            await FollowupAsync("❌ Failed to set up ticket system. Please try again.", ephemeral: true);
        }
    }

    [SlashCommand("close", "Close the current ticket")]
    public async Task CloseAsync()
    {
        await DeferAsync(ephemeral: true);

        var (success, message) = await _service.CloseTicketAsync(
            Context.Channel.Id,
            Context.User.Id);

        await FollowupAsync(success ? $"✅ {message}" : $"❌ {message}", ephemeral: true);
    }

    [SlashCommand("add", "Add a user to the current ticket")]
    [DefaultMemberPermissions(GuildPermission.ManageChannels)]
    public async Task AddUserAsync(
        [Summary("user", "User to add to this ticket")] IGuildUser user)
    {
        await DeferAsync(ephemeral: true);

        var channel = Context.Channel as Discord.WebSocket.SocketTextChannel;
        if (channel is null)
        {
            await FollowupAsync("❌ Cannot modify permissions here.", ephemeral: true);
            return;
        }

        await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow,
            readMessageHistory: PermValue.Allow));

        await FollowupAsync($"✅ Added {user.Mention} to this ticket.", ephemeral: true);
    }

    [SlashCommand("remove", "Remove a user from the current ticket")]
    [DefaultMemberPermissions(GuildPermission.ManageChannels)]
    public async Task RemoveUserAsync(
        [Summary("user", "User to remove from this ticket")] IGuildUser user)
    {
        await DeferAsync(ephemeral: true);

        var channel = Context.Channel as Discord.WebSocket.SocketTextChannel;
        if (channel is null)
        {
            await FollowupAsync("❌ Cannot modify permissions here.", ephemeral: true);
            return;
        }

        await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(
            viewChannel: PermValue.Deny));

        await FollowupAsync($"✅ Removed {user.Mention} from this ticket.", ephemeral: true);
    }
}