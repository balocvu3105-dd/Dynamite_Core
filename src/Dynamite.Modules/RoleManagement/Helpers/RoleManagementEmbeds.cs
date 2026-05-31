// src/Dynamite.Modules/RoleManagement/Helpers/RoleManagementEmbeds.cs
namespace Dynamite.Modules.RoleManagement.Helpers;

using Discord;

public static class RoleManagementEmbeds
{
    private static readonly Color SuccessColor = new(0x57F287);
    private static readonly Color ErrorColor = new(0xED4245);
    private static readonly Color WarnColor = new(0xFEE75C);
    private static readonly Color InfoColor = new(0x5865F2);

    public static Embed Success(string title, string description) => Build(title, description, SuccessColor);
    public static Embed Error(string title, string description) => Build(title, description, ErrorColor);
    public static Embed Warn(string title, string description) => Build(title, description, WarnColor);
    public static Embed Info(string title, string description) => Build(title, description, InfoColor);

    private static Embed Build(string title, string description, Color color)
        => new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
}