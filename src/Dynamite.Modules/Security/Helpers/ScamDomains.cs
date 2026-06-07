// src/Dynamite.Modules/Security/Helpers/ScamDomains.cs
namespace Dynamite.Modules.Security.Helpers;

/// <summary>
/// Known scam/phishing domains commonly used in Discord scams.
/// Keep this list conservative — false positives are worse than misses.
/// </summary>
public static class ScamDomains
{
    private static readonly HashSet<string> Domains = new(StringComparer.OrdinalIgnoreCase)
    {
        // Discord nitro scams
        "discordnitro.free",
        "discord-nitro.gift",
        "discordgift.site",
        "discord-gift.net",
        "dlscord.com",
        "discorcl.com",
        "discorb.com",

        // Steam scams
        "steamcommunity.ru",
        "stearn.com",
        "steam-trade.net",

        // Generic phishing patterns — domains containing these substrings
    };

    // Substrings yang sering muncul di scam links
    private static readonly string[] SuspiciousPatterns =
    [
        "free-nitro",
        "discord-gift",
        "nitro-free",
        "steamcornmunity",
        "csgoskins.free",
    ];

    public static bool IsScamLink(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var host = uri.Host.ToLower();

        // Check exact domain match
        if (Domains.Contains(host)) return true;

        // Check suspicious patterns in URL
        var lowerUrl = url.ToLower();
        foreach (var pattern in SuspiciousPatterns)
        {
            if (lowerUrl.Contains(pattern)) return true;
        }

        return false;
    }
}