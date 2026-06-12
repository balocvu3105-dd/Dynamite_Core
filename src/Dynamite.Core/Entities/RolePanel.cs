// src/Dynamite.Core/Entities/RolePanel.cs
namespace Dynamite.Core.Entities;

using Dynamite.Core.Enums;

public class RolePanel : BaseEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RolePanelType PanelType { get; set; }

    // Số role TỐI ĐA user được giữ từ panel này. 0 = không giới hạn.
    // Vd: panel "hệ phái" MaxRoles = 2 → đang giữ 2 thì phải gỡ bớt mới nhận thêm.
    public int MaxRoles { get; set; } = 0;

    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;

    public ICollection<RolePanelItem> Items { get; set; } = [];
}