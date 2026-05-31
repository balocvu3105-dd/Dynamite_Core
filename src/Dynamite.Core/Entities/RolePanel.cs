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

    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;

    public ICollection<RolePanelItem> Items { get; set; } = [];
}