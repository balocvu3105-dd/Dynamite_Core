// src/Dynamite.Modules.Economy/Helpers/FishingChannelGuard.cs
namespace Dynamite.Modules.Economy.Helpers;

using Discord.Interactions;
using Dynamite.Core.Interfaces.Repositories;

/// <summary>
/// Guard chung cho tất cả lệnh câu cá — chặn nếu dùng sai channel.
/// Gọi TRƯỚC khi DeferAsync/RespondAsync.
/// Nếu FishingChannelId chưa được set → cho phép mọi channel.
/// </summary>
public static class FishingChannelGuard
{
    public static async Task<bool> CheckAsync(
        SocketInteractionContext ctx,
        IGuildConfigRepository   configRepo)
    {
        var config = await configRepo.GetByGuildIdAsync(ctx.Guild.Id);

        // Bãi câu đang đóng cửa
        if (config is not null && !config.FishingEnabled)
        {
            await ctx.Interaction.RespondAsync(
                "🚫 **Bãi câu đang đóng cửa!** Admin đã tạm thời đóng khu vực câu cá.\n" +
                "Vui lòng đợi thông báo mở lại.",
                ephemeral: true);
            return false;
        }

        if (config?.FishingChannelId is null) return true;
        if (ctx.Channel.Id == config.FishingChannelId) return true;

        await ctx.Interaction.RespondAsync(
            $"❌ Lệnh câu cá chỉ dùng được trong <#{config.FishingChannelId}>!",
            ephemeral: true);
        return false;
    }
}
