// src/Dynamite.Core/Entities/AntiSpamConfig.cs
namespace Dynamite.Core.Entities;

public class AntiSpamConfig : BaseEntity
{
    public ulong GuildId { get; set; }
    public Guid GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; } = null!;

    public bool Enabled { get; set; } = false;

    // Spam: X messages trong Y seconds
    public int MessageThreshold { get; set; } = 5;
    public int MessageWindowSeconds { get; set; } = 5;

    // Mention spam
    public int MentionThreshold { get; set; } = 5;

    // Feature flags
    public bool AntiInvite { get; set; } = false;
    public bool AntiScamLink { get; set; } = false;
    public bool AntiRaid { get; set; } = false;

    // Anti-raid: X joins trong 10 seconds thì lockdown
    public int RaidThreshold { get; set; } = 10;
}