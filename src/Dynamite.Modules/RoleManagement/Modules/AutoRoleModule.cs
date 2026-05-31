// src/Dynamite.Modules/RoleManagement/Modules/AutoRoleModule.cs
namespace Dynamite.Modules.RoleManagement.Modules;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.RoleManagement.Helpers;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.ManageRoles)]
[Group("autorole", "Manage automatic role assignment on member join")]
public class AutoRoleModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAutoRoleService _autoRoleService;

    public AutoRoleModule(IAutoRoleService autoRoleService)
        => _autoRoleService = autoRoleService;

    [SlashCommand("add", "Add a role to be automatically given when a member joins")]
    public async Task AddAsync(
        [Summary("role", "The role to assign on join")] IRole role)
    {
        await DeferAsync(ephemeral: true);

        var botUser = Context.Guild.CurrentUser;

        // Validate: bot có ManageRoles không?
        if (!botUser.GuildPermissions.ManageRoles)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Missing Permission",
                "I need the **Manage Roles** permission to assign roles."), ephemeral: true);
            return;
        }

        // Validate: role của bot phải cao hơn role muốn assign
        if (botUser.Hierarchy <= role.Position)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Role Too High",
                $"My highest role must be above {role.Mention} to assign it."), ephemeral: true);
            return;
        }

        // Validate: không gán @everyone
        if (role.Id == Context.Guild.EveryoneRole.Id)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Invalid Role",
                "You cannot set @everyone as an auto role."), ephemeral: true);
            return;
        }

        await _autoRoleService.AddAutoRoleAsync(
            Context.Guild.Id, Context.Guild.Name, role.Id);

        await FollowupAsync(embed: RoleManagementEmbeds.Success(
            "Auto Role Added",
            $"{role.Mention} will now be assigned to new members when they join."), ephemeral: true);
    }

    [SlashCommand("remove", "Remove a role from automatic assignment")]
    public async Task RemoveAsync(
        [Summary("role", "The role to remove from auto assignment")] IRole role)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            await _autoRoleService.RemoveAutoRoleAsync(Context.Guild.Id, role.Id);
            await FollowupAsync(embed: RoleManagementEmbeds.Success(
                "Auto Role Removed",
                $"{role.Mention} will no longer be assigned automatically."), ephemeral: true);
        }
        catch (InvalidOperationException)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Error(
                "Not Found",
                $"{role.Mention} is not configured as an auto role."), ephemeral: true);
        }
    }

    [SlashCommand("list", "Show all configured auto roles")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);

        var autoRoles = (await _autoRoleService.GetAutoRolesAsync(Context.Guild.Id)).ToList();

        if (autoRoles.Count == 0)
        {
            await FollowupAsync(embed: RoleManagementEmbeds.Info(
                "No Auto Roles",
                "No auto roles configured. Use `/autorole add` to add one."), ephemeral: true);
            return;
        }

        var roleList = string.Join("\n", autoRoles.Select(r => $"• <@&{r.RoleId}>"));
        await FollowupAsync(embed: RoleManagementEmbeds.Info(
            $"Auto Roles ({autoRoles.Count})", roleList), ephemeral: true);
    }
}