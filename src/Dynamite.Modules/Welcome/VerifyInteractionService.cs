// src/Dynamite.Modules/Welcome/VerifyInteractionService.cs
namespace Dynamite.Modules.Welcome;

using Discord;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Modules.Welcome.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class VerifyInteractionService
{
    public const string VerifyButtonId = "verify:confirm";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VerifyInteractionService> _logger;

    public VerifyInteractionService(
        IServiceScopeFactory scopeFactory,
        ILogger<VerifyInteractionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleVerifyAsync(SocketMessageComponent interaction)
    {
        // 1. Luôn DeferAsync ngay lập tức trước bất kỳ xử lý nào để tránh lỗi "Tương tác này không thành công" (timeout 3s)
        await interaction.DeferAsync(ephemeral: true);

        // 2. Resolve IGuildUser an toàn kể cả khi Discord cache chưa kịp tải user
        var guildUser = interaction.User as IGuildUser;
        if (guildUser is null && interaction.Channel is IGuildChannel guildChannel)
        {
            guildUser = await guildChannel.Guild.GetUserAsync(interaction.User.Id);
        }

        if (guildUser is null)
        {
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Error(
                    "Error",
                    "Could not resolve your member profile on this server. Please try again or contact an admin."),
                ephemeral: true);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var welcomeService = scope.ServiceProvider.GetRequiredService<IWelcomeService>();

        var (roleId, configured) = await welcomeService.GetVerifyRoleAsync(guildUser.Guild.Id);

        if (!configured || roleId is null)
        {
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Error(
                    "Not Configured",
                    "Verification role has not been set up in the database. Please contact an admin to run `/verify set-role`."),
                ephemeral: true);
            return;
        }

        // Kiểm tra role có còn tồn tại trên Discord không
        var role = guildUser.Guild.GetRole(roleId.Value);
        if (role is null)
        {
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Error(
                    "Role Not Found",
                    "The configured verification role no longer exists on this Discord server. Please contact an admin to re-run `/verify set-role`."),
                ephemeral: true);
            return;
        }

        // Kiểm tra quyền hạn/phân cấp role của Bot
        var socketGuild = ((SocketGuildChannel)interaction.Channel).Guild;
        var botUser = socketGuild.CurrentUser;
        if (botUser is not null && botUser.Hierarchy <= role.Position)
        {
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Error(
                    "Permission Error",
                    $"My bot role is below **@{role.Name}** in Server Settings → Roles. Please ask an admin to drag the bot's role higher!"),
                ephemeral: true);
            return;
        }

        // Cek apakah sudah punya role
        if (guildUser.RoleIds.Contains(roleId.Value))
        {
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Info(
                    "Already Verified",
                    "You are already verified!"),
                ephemeral: true);
            return;
        }

        try
        {
            await guildUser.AddRoleAsync(roleId.Value);

            // Thu hồi role cũ (vd: role "khách") nếu được cấu hình.
            // Gỡ SAU khi gán role mới để user không bao giờ rơi vào trạng thái
            // không có role nào → mất quyền xem channel giữa chừng.
            // Lỗi ở bước gỡ không được làm hỏng verify — chỉ log warning.
            var removeRoleId = await welcomeService.GetVerifyRemoveRoleAsync(guildUser.Guild.Id);
            if (removeRoleId is not null && guildUser.RoleIds.Contains(removeRoleId.Value))
            {
                try
                {
                    await guildUser.RemoveRoleAsync(removeRoleId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Verified user {UserId} but failed to remove role {RoleId} in guild {GuildId}",
                        guildUser.Id, removeRoleId.Value, guildUser.Guild.Id);
                }
            }

            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Success(
                    "✅ Verified!",
                    $"Welcome to **{guildUser.Guild.Name}**! You now have access to the server."),
                ephemeral: true);

            _logger.LogInformation("User {UserId} verified in guild {GuildId}",
                guildUser.Id, guildUser.Guild.Id);
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == Discord.DiscordErrorCode.MissingPermissions)
        {
            _logger.LogError(ex, "Missing permissions when assigning verify role {RoleId} to user {UserId}", roleId.Value, guildUser.Id);
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Error(
                    "Missing Permissions",
                    "The bot does not have permission (`Manage Roles`) or proper hierarchy to assign the verified role. Please contact an admin."),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign verify role to user {UserId}", guildUser.Id);
            await interaction.FollowupAsync(
                embed: WelcomeEmbeds.Error(
                    "Error",
                    $"Failed to assign the verified role: {ex.Message}. Please contact an admin."),
                ephemeral: true);
        }
    }
}