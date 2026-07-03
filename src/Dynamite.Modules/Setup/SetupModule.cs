// src/Dynamite.Modules/Setup/SetupModule.cs
namespace Dynamite.Modules.Setup;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Modules.Setup.Services;
using Dynamite.Modules.Setup.Templates;
using Microsoft.Extensions.Logging;

// ────────────────────────────────────────────────────────────────────────────
// SetupModule — slash commands /setup gaming|community|streamer|smart
// ────────────────────────────────────────────────────────────────────────────

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
[RequireBotPermission(GuildPermission.ManageChannels | GuildPermission.ManageRoles)]
[Group("setup", "Automatically set up your server with a preset template")]
public class SetupModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly SetupExecutor _executor;
    private readonly SmartSetupEngine _smartEngine;
    private readonly ILogger<SetupModule> _logger;

    public SetupModule(SetupExecutor executor, SmartSetupEngine smartEngine, ILogger<SetupModule> logger)
    {
        _executor = executor;
        _smartEngine = smartEngine;
        _logger = logger;
    }

    [SlashCommand("gaming", "Set up a gaming community server")]
    public async Task SetupGamingAsync()
        => await RunSetupAsync(GamingTemplate.Create());

    [SlashCommand("community", "Set up a general community server")]
    public async Task SetupCommunityAsync()
        => await RunSetupAsync(CommunityTemplate.Create());

    [SlashCommand("streamer", "Set up a streamer community server")]
    public async Task SetupStreamerAsync()
        => await RunSetupAsync(StreamerTemplate.Create());

    [SlashCommand("smart", "Smart wizard setup tailored to your community topic and scale")]
    public async Task SetupSmartAsync(
        [Summary("topic", "Main server theme or topic")] SmartServerTopic topic = SmartServerTopic.Community,
        [Summary("scale", "Expected server member size")] SmartServerScale scale = SmartServerScale.Medium,
        [Summary("economy", "Include economy and fishing channels")] bool economy = true,
        [Summary("ticket", "Include support ticket channels")] bool ticket = true,
        [Summary("moderation", "Include staff moderation channels")] bool moderation = true,
        [Summary("voice", "Include voice lounges")] bool voice = true)
    {
        var options = new SmartSetupOptions
        {
            Topic = topic,
            Scale = scale,
            EnableEconomy = economy,
            EnableTicket = ticket,
            EnableModeration = moderation,
            EnableVoice = voice
        };

        var template = _smartEngine.GenerateTemplate(options);
        await RunSetupAsync(template);
    }

    private async Task RunSetupAsync(SetupTemplate template)
    {
        await DeferAsync(ephemeral: false);

        _logger.LogInformation("Setup '{Template}' started by {UserId} in guild {GuildId}",
            template.Name, Context.User.Id, Context.Guild.Id);

        var progressMessage = await FollowupAsync(
            embed: BuildProgressEmbed($"🔧 Setting up **{template.Name}** server...\n\nStarting..."));

        async Task ProgressCallback(string status)
        {
            try { await progressMessage.ModifyAsync(m => m.Embed = BuildProgressEmbed(status)); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to update progress message"); }
        }

        var result = await _executor.ExecuteAsync((SocketGuild)Context.Guild, template, ProgressCallback);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Setup '{Template}' succeeded for guild {GuildId}",
                template.Name, Context.Guild.Id);
            await progressMessage.ModifyAsync(m => m.Embed = BuildSuccessEmbed(template, result));
        }
        else if (result.IsCancelled)
        {
            await progressMessage.ModifyAsync(m => m.Embed = BuildErrorEmbed(
                "Setup Cancelled", "The setup was cancelled. All created channels and roles have been removed."));
        }
        else
        {
            _logger.LogError("Setup '{Template}' failed for guild {GuildId}: {Error}",
                template.Name, Context.Guild.Id, result.ErrorMessage);
            await progressMessage.ModifyAsync(m => m.Embed = BuildErrorEmbed(
                "Setup Failed",
                $"An error occurred and the setup was rolled back.\n\n**Error:** {result.ErrorMessage}"));
        }
    }

    private static Embed BuildProgressEmbed(string status)
        => new EmbedBuilder()
            .WithTitle("🔧 Server Setup")
            .WithDescription(status)
            .WithColor(new Color(0xFEE75C))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    private static Embed BuildSuccessEmbed(SetupTemplate template, SetupResult result)
    {
        var b = new EmbedBuilder()
            .WithTitle("✅ Server Setup Complete")
            .WithDescription($"**{template.Name}** template applied successfully!")
            .WithColor(new Color(0x57F287))
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (result.CreatedRoles > 0) b.AddField("Roles Created", result.CreatedRoles.ToString(), inline: true);
        if (result.CreatedCategories > 0) b.AddField("Categories Created", result.CreatedCategories.ToString(), inline: true);
        if (result.CreatedChannels > 0) b.AddField("Channels Created", result.CreatedChannels.ToString(), inline: true);

        b.AddField("ℹ️ Note",
            "Existing channels and roles were not modified. You can customize everything from Server Settings.");

        return b.Build();
    }

    private static Embed BuildErrorEmbed(string title, string description)
        => new EmbedBuilder()
            .WithTitle($"❌ {title}")
            .WithDescription(description)
            .WithColor(new Color(0xED4245))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
}

// ────────────────────────────────────────────────────────────────────────────
// SetupExecutor — thực thi Discord API calls, progress reporting, rollback
// ────────────────────────────────────────────────────────────────────────────

public class SetupExecutor
{
    private readonly ILogger<SetupExecutor> _logger;

    public SetupExecutor(ILogger<SetupExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<SetupResult> ExecuteAsync(
        SocketGuild guild,
        SetupTemplate template,
        Func<string, Task> progressCallback,
        CancellationToken ct = default)
    {
        var createdRoles = new List<IRole>();
        var createdCategories = new List<ICategoryChannel>();
        var createdChannels = new List<IGuildChannel>();

        try
        {
            // ── Step 1: Roles ──────────────────────────────────────────────
            await progressCallback("⚙️ Creating roles...");

            var roleMap = new Dictionary<string, IRole>(StringComparer.OrdinalIgnoreCase)
            {
                ["@everyone"] = guild.EveryoneRole
            };
            foreach (var r in guild.Roles)
                roleMap.TryAdd(r.Name, r);

            foreach (var roleDef in template.Roles)
            {
                ct.ThrowIfCancellationRequested();

                if (roleMap.ContainsKey(roleDef.Name))
                {
                    _logger.LogInformation("Role '{Name}' already exists — skipping", roleDef.Name);
                    continue;
                }

                GuildPermissions? perms = roleDef.Permissions.HasValue
                    ? new GuildPermissions((ulong)roleDef.Permissions.Value)
                    : null;

                var role = await guild.CreateRoleAsync(
                    roleDef.Name, perms, roleDef.Color, roleDef.Hoisted, roleDef.Mentionable);

                createdRoles.Add(role);
                roleMap[role.Name] = role;
                _logger.LogInformation("Created role '{Name}'", role.Name);
            }

            await progressCallback("✅ Roles ready.\n⚙️ Creating channels...");

            // ── Step 2: Categories + Channels ─────────────────────────────
            var totalChannels = template.Categories.Sum(c => c.Channels.Count);
            var doneChannels = 0;

            foreach (var categoryDef in template.Categories)
            {
                ct.ThrowIfCancellationRequested();

                var existingCategory = guild.CategoryChannels
                    .FirstOrDefault(c => c.Name.Equals(categoryDef.Name, StringComparison.OrdinalIgnoreCase));

                ICategoryChannel category;

                if (existingCategory is not null)
                {
                    _logger.LogInformation("Category '{Name}' already exists — skipping", categoryDef.Name);
                    category = existingCategory;
                }
                else
                {
                    var overwrites = BuildOverwrites(categoryDef.Overwrites, roleMap);
                    category = await guild.CreateCategoryChannelAsync(categoryDef.Name, props =>
                    {
                        props.PermissionOverwrites = overwrites;
                    });
                    createdCategories.Add(category);
                    _logger.LogInformation("Created category '{Name}'", categoryDef.Name);
                }

                foreach (var channelDef in categoryDef.Channels)
                {
                    ct.ThrowIfCancellationRequested();

                    var alreadyExists = guild.Channels
                        .OfType<INestedChannel>()
                        .Any(c => c.CategoryId == category.Id
                            && c.Name.Equals(SanitizeName(channelDef.Name), StringComparison.OrdinalIgnoreCase));

                    if (alreadyExists)
                    {
                        _logger.LogInformation("Channel '{Name}' already exists — skipping", channelDef.Name);
                        doneChannels++;
                        continue;
                    }

                    var channelOverwrites = BuildOverwrites(channelDef.Overwrites, roleMap);
                    IGuildChannel channel;

                    if (channelDef.Type == ChannelType.Voice)
                    {
                        channel = await guild.CreateVoiceChannelAsync(channelDef.Name, props =>
                        {
                            props.CategoryId = category.Id;
                            if (channelOverwrites.Count > 0)
                                props.PermissionOverwrites = channelOverwrites;
                        });
                    }
                    else
                    {
                        channel = await guild.CreateTextChannelAsync(channelDef.Name, props =>
                        {
                            props.CategoryId = category.Id;
                            if (!string.IsNullOrWhiteSpace(channelDef.Topic))
                                props.Topic = channelDef.Topic;
                            if (channelDef.SlowModeInterval > 0)
                                props.SlowModeInterval = channelDef.SlowModeInterval;
                            if (channelOverwrites.Count > 0)
                                props.PermissionOverwrites = channelOverwrites;
                        });
                    }

                    createdChannels.Add(channel);
                    doneChannels++;
                    _logger.LogInformation("Created channel '{Name}' in '{Category}'",
                        channelDef.Name, categoryDef.Name);
                }

                await progressCallback(
                    $"✅ Roles ready.\n⚙️ Channels: {doneChannels}/{totalChannels} created...");
            }

            _logger.LogInformation(
                "Setup '{Template}' complete — {Roles} roles, {Cats} categories, {Ch} channels",
                template.Name, createdRoles.Count, createdCategories.Count, createdChannels.Count);

            return SetupResult.Success(createdRoles, createdCategories, createdChannels);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Setup cancelled for guild {GuildId} — rolling back", guild.Id);
            await RollbackAsync(createdRoles, createdCategories, createdChannels);
            return SetupResult.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Setup failed for guild {GuildId} — rolling back", guild.Id);
            await RollbackAsync(createdRoles, createdCategories, createdChannels);
            return SetupResult.Failure(ex.Message);
        }
    }

    private async Task RollbackAsync(
        List<IRole> roles,
        List<ICategoryChannel> categories,
        List<IGuildChannel> channels)
    {
        foreach (var ch in channels)
        {
            try { await ch.DeleteAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Rollback: failed to delete channel {Id}", ch.Id); }
        }
        foreach (var cat in categories)
        {
            try { await cat.DeleteAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Rollback: failed to delete category {Id}", cat.Id); }
        }
        foreach (var role in roles)
        {
            try { await role.DeleteAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Rollback: failed to delete role {Id}", role.Id); }
        }
    }

    private static List<Overwrite> BuildOverwrites(
        IReadOnlyList<PermissionOverwriteTemplate> overwrites,
        Dictionary<string, IRole> roleMap)
    {
        var result = new List<Overwrite>();
        foreach (var ow in overwrites)
        {
            if (!roleMap.TryGetValue(ow.TargetRoleName, out var role)) continue;
            result.Add(new Overwrite(role.Id, PermissionTarget.Role, ow.Permissions));
        }
        return result;
    }

    private static string SanitizeName(string name)
        => name.ToLowerInvariant().Replace(' ', '-');
}

// ────────────────────────────────────────────────────────────────────────────
// SetupResult — result object, tránh throw exception ra ngoài Executor
// ────────────────────────────────────────────────────────────────────────────

public sealed class SetupResult
{
    public bool IsSuccess { get; private init; }
    public bool IsCancelled { get; private init; }
    public string? ErrorMessage { get; private init; }

    public int CreatedRoles { get; private init; }
    public int CreatedCategories { get; private init; }
    public int CreatedChannels { get; private init; }

    public static SetupResult Success(
        IReadOnlyList<IRole> roles,
        IReadOnlyList<ICategoryChannel> categories,
        IReadOnlyList<IGuildChannel> channels)
        => new()
        {
            IsSuccess = true,
            CreatedRoles = roles.Count,
            CreatedCategories = categories.Count,
            CreatedChannels = channels.Count
        };

    public static SetupResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };

    public static SetupResult Cancelled()
        => new() { IsSuccess = false, IsCancelled = true };
}