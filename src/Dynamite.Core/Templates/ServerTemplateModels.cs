// src/Dynamite.Core/Templates/ServerTemplateModels.cs
namespace Dynamite.Core.Templates;

/// <summary>
/// Defines the structure of a server template.
/// All Discord-specific types are excluded — this is pure domain data.
/// </summary>
public record ServerTemplateDefinition(
    string TemplateName,
    string Description,
    IReadOnlyList<RoleDefinition> Roles,
    IReadOnlyList<CategoryDefinition> Categories
);

public record RoleDefinition(
    string Name,
    uint Color,           // hex color, e.g. 0xFF5733
    bool Mentionable,
    bool Hoisted,         // show separately in member list
    int Position,         // relative position hint (lower = higher in list)
    RolePermissions Permissions
);

public record RolePermissions(
    bool SendMessages = true,
    bool ReadMessages = true,
    bool Connect = true,
    bool Speak = true,
    bool UseVoiceActivity = true,
    bool AddReactions = true,
    bool UseExternalEmojis = true,
    bool AttachFiles = true,
    bool EmbedLinks = true,
    bool ReadMessageHistory = true
);

public record CategoryDefinition(
    string Name,
    IReadOnlyList<ChannelDefinition> Channels
);

public record ChannelDefinition(
    string Name,
    ChannelKind Kind,
    string? Topic = null,
    bool IsNsfw = false,
    int? SlowmodeSeconds = null,
    int? UserLimit = null      // voice only
);

public enum ChannelKind
{
    Text,
    Voice,
    Announcement, // news channel
    Forum
}