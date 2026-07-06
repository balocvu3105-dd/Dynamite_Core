// src/Dynamite.Core/Entities/ServerActivityLog.cs
namespace Dynamite.Core.Entities;

using Dynamite.Core.Enums;

public class ServerActivityLog : BaseEntity
{
    public ulong GuildId { get; set; }
    public LogCategory Category { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? ActorUsername { get; set; }
    public string? ActorAvatarUrl { get; set; }
    public string? TargetId { get; set; }
    public string? TargetUsername { get; set; }
    public string? Metadata { get; set; } // JSON formatted string for extra details
}
