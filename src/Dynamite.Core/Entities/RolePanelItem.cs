// src/Dynamite.Core/Entities/RolePanelItem.cs
namespace Dynamite.Core.Entities;

public class RolePanelItem : BaseEntity
{
    public ulong RoleId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Emoji { get; set; }       // e.g. "🎮" hoặc "<:custom:123>"
    public string? Description { get; set; } // chỉ hiện trong Select Menu

    public Guid RolePanelId { get; set; }
    public RolePanel RolePanel { get; set; } = null!;
}