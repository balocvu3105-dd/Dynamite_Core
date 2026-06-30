namespace Dynamite.Core.Enums;

public enum ModerationActionType
{
    Warn = 0,
    Kick = 1,
    Ban = 2,
    Unban = 3,
    Timeout = 4,
    Untimeout = 5,

    /// <summary>Ban issued by user ID (target was not in the server at the time).</summary>
    BanId = 6,

    /// <summary>User added to the permanent blacklist.</summary>
    Blacklist = 7,

    /// <summary>User removed from the permanent blacklist.</summary>
    Unblacklist = 8
}
