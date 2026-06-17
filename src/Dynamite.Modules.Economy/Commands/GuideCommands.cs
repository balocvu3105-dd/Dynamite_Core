// src/Dynamite.Modules.Economy/Commands/GuideCommands.cs
namespace Dynamite.Modules.Economy.Commands;

using Discord;
using Discord.Interactions;
using Dynamite.Modules.Economy.Services;

/// <summary>
/// /guide — Quản lý kênh cẩm nang hướng dẫn.
/// </summary>
[Group("guide", "Quản lý cẩm nang hướng dẫn server")]
[RequireContext(ContextType.Guild)]
public class GuideCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GuideService _guide;

    public GuideCommands(GuideService guide)
    {
        _guide = guide;
    }

    // ── /guide set-channel ────────────────────────────────────────────────────

    [SlashCommand("set-channel", "Đặt kênh cẩm nang hướng dẫn (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetChannelAsync(
        [Summary("channel", "Channel sẽ đăng cẩm nang hướng dẫn")] ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        var (ok, msg) = await _guide.SetChannelAsync(
            Context.Guild.Id, Context.Guild.Name, channel);

        await FollowupAsync(msg, ephemeral: true);
    }

    // ── /guide post ───────────────────────────────────────────────────────────

    [SlashCommand("post", "Đăng / cập nhật cẩm nang hướng dẫn vào channel đã cài đặt (Admin)")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task PostAsync()
    {
        await DeferAsync(ephemeral: true);

        var (ok, msg) = await _guide.PostGuideAsync(Context.Guild.Id);
        await FollowupAsync(ok ? msg : $"❌ {msg}", ephemeral: true);
    }
}
