// src/Dynamite.Bot/Services/BotHostedService.cs
namespace Dynamite.Bot.Services;

using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Dynamite.Application.Interfaces;
using Dynamite.Bot.Settings;
using Dynamite.Core.Enums;
using Dynamite.Modules.Giveaway.Interactions;
using Dynamite.Modules.Logging;
using Dynamite.Modules.Logging.Helpers;
using Dynamite.Modules.RoleManagement.Services;
using Dynamite.Modules.Security;
using Dynamite.Modules.Ticket.Interactions;
using Dynamite.Modules.Economy.Handlers;
using Dynamite.Modules.Voice;
using Dynamite.Modules.Logging.Loggers;
using Dynamite.Modules.Moderation.Services;
using Dynamite.Modules.Welcome;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class BotHostedService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly DiscordSettings _settings;
    private readonly GuildPresenceSyncService _presenceSync;
    private readonly BotStatusProvider _statusProvider;
    private readonly ILogger<BotHostedService> _logger;

    private readonly ModAuditLogger _modAuditLogger;
    private bool _modulesLoaded = false;
    private bool _startupNotificationSent = false;
    private bool _crashedLastTime = false;

    private static readonly IReadOnlyList<Assembly> ModuleAssemblies =
    [
        Assembly.GetExecutingAssembly(),
        typeof(Dynamite.Modules.Moderation.Modules.ModerationModule).Assembly,
        typeof(Dynamite.Modules.Moderation.Modules.ConfigModule).Assembly,
        typeof(Dynamite.Modules.RoleManagement.Modules.AutoRoleModule).Assembly,
        typeof(Dynamite.Modules.Logging.Modules.LogConfigModule).Assembly,
        typeof(Dynamite.Modules.Welcome.Modules.WelcomeConfigModule).Assembly,
        typeof(Dynamite.Modules.Security.Modules.AntiSpamConfigModule).Assembly,
        typeof(Dynamite.Modules.Setup.SetupModule).Assembly,
        typeof(Dynamite.Modules.Giveaway.Commands.GiveawayCommands).Assembly,
        typeof(Dynamite.Modules.Ticket.Commands.TicketCommands).Assembly,
        typeof(Dynamite.Modules.Economy.Commands.EconomyCommands).Assembly,
        typeof(Dynamite.Modules.Voice.Commands.TempVoiceModule).Assembly,
    ];

    public BotHostedService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IOptions<DiscordSettings> settings,
        GuildPresenceSyncService presenceSync,
        BotStatusProvider statusProvider,
        ModAuditLogger modAuditLogger,
        ILogger<BotHostedService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _settings = settings.Value;
        _presenceSync = presenceSync;
        _statusProvider = statusProvider;
        _modAuditLogger = modAuditLogger;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // ── Check Crash State Marker ──
        try
        {
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);
            var runningFlagPath = Path.Combine(logsDir, "bot_running.flag");
            var cleanFlagPath = Path.Combine(logsDir, "clean_shutdown.flag");

            if (File.Exists(runningFlagPath) && !File.Exists(cleanFlagPath))
            {
                _crashedLastTime = true;
                _logger.LogWarning("Crash detected! Previous session ended abruptly without clean shutdown.");
            }
            else
            {
                _crashedLastTime = false;
            }

            File.WriteAllText(runningFlagPath, DateTime.UtcNow.ToString("O"));
            if (File.Exists(cleanFlagPath))
            {
                File.Delete(cleanFlagPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not update session state flags");
        }

        _client.Log += LogAsync;
        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;
        _client.UserJoined += OnUserJoinedAsync;
        _client.ButtonExecuted += OnButtonExecutedAsync;
        _client.SelectMenuExecuted += OnSelectMenuExecutedAsync;
        _client.ModalSubmitted += OnModalSubmittedAsync;

        _interactions.Log += LogAsync; // InteractionService có Log event RIÊNG — không hook là mất hết exception nội bộ
        _interactions.InteractionExecuted += OnInteractionExecutedAsync;

        _client.JoinedGuild += guild => _presenceSync.OnGuildJoinedAsync(guild);
        _client.LeftGuild += guild => _presenceSync.OnGuildLeftAsync(guild);

        await _client.LoginAsync(TokenType.Bot, _settings.Token);
        await _client.StartAsync();

        _logger.LogInformation("Bot started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _statusProvider.SetNotReady();
        _logger.LogInformation("Bot shutting down — sending audit log notifications...");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await SendShutdownNotificationsAsync(cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Some shutdown notifications could not be sent");
        }

        // ── Mark Clean Shutdown Flag ──
        try
        {
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);
            var runningFlagPath = Path.Combine(logsDir, "bot_running.flag");
            var cleanFlagPath = Path.Combine(logsDir, "clean_shutdown.flag");

            File.WriteAllText(cleanFlagPath, DateTime.UtcNow.ToString("O"));
            if (File.Exists(runningFlagPath))
            {
                File.Delete(runningFlagPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not write clean shutdown flag");
        }

        await _client.StopAsync();
        _logger.LogInformation("Bot stopped");
    }

    private async Task SendShutdownNotificationsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();

            var embed = new EmbedBuilder()
                .WithTitle("🔴 Bot Shutting Down")
                .WithDescription("Dynamite is going offline for maintenance or scheduled restart.\nIt will be back online shortly.")
                .WithColor(new Color(0xED4245))
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter("Dynamite | Graceful Shutdown")
                .Build();

            foreach (var guild in _client.Guilds)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var auditChannelId = await logService.GetLogChannelAsync(guild.Id, LogCategory.Audit, ct);
                    if (auditChannelId is null) continue;

                    var channel = guild.GetTextChannel(auditChannelId.Value);
                    if (channel is null) continue;

                    await channel.SendMessageAsync(embed: embed);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not send shutdown notification to guild {GuildId}", guild.Id);
                }
            }

            _logger.LogInformation("Shutdown notifications sent");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send shutdown notifications");
        }
    }

    private async Task SendStartupNotificationsAsync(bool crashedLastTime, CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();

            var title = crashedLastTime
                ? "⚠️ Dynamite Vừa Khởi Động Lại Sau Sự Cố"
                : "🟢 Dynamite Đã Khởi Động Thành Công";

            var description = crashedLastTime
                ? "Bot vừa phục hồi sau một sự cố sập đột ngột (Crash / Mất kết nối máy chủ / OOM).\nToàn bộ hệ thống và lệnh Slash đã hoạt động trở lại bình thường."
                : "Bot đã hoàn tất quá trình khởi động và sẵn sàng hoạt động sau bảo trì hoặc khởi động định kỳ.";

            var color = crashedLastTime ? new Color(0xFEE75C) : new Color(0x57F287);

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color)
                .AddField("🕒 Thời gian", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:F> (<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>)")
                .AddField("⚙️ Trạng thái", "🟢 Sẵn sàng phục vụ", true)
                .AddField("🔢 Slash Commands", $"{_interactions.SlashCommands.Count} lệnh", true)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter(crashedLastTime ? "Dynamite | Auto Crash Recovery Notify" : "Dynamite | Startup Notification")
                .Build();

            int sentCount = 0;
            foreach (var guild in _client.Guilds)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var auditChannelId = await logService.GetLogChannelAsync(guild.Id, LogCategory.Audit, ct);
                    if (auditChannelId is null) continue;

                    var channel = guild.GetTextChannel(auditChannelId.Value);
                    if (channel is null) continue;

                    await channel.SendMessageAsync(embed: embed);
                    sentCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not send startup notification to guild {GuildId}", guild.Id);
                }
            }

            _logger.LogInformation("Startup notifications sent to {Count} guilds (CrashDetected={CrashDetected})", sentCount, crashedLastTime);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send startup notifications");
        }
    }

    private async Task OnReadyAsync()
    {
        try
        {
            // ── 1. One-time init: load modules + subscribe event handlers ──
            // Ready có thể fire lại khi reconnect — không được subscribe trùng
            if (!_modulesLoaded)
            {
                var distinct = ModuleAssemblies.Distinct();
                foreach (var assembly in distinct)
                {
                    await _interactions.AddModulesAsync(assembly, _services);
                    _logger.LogDebug("Loaded interaction modules from {Assembly}", assembly.GetName().Name);
                }

                _services.GetRequiredService<LoggingEventHandler>().Subscribe();
                _services.GetRequiredService<WelcomeEventHandler>().Subscribe();
                _services.GetRequiredService<SecurityEventHandler>().Subscribe();
                _services.GetRequiredService<TempVoiceEventHandler>().Subscribe();
                _services.GetRequiredService<EconomyEventHandler>().Subscribe();
                _services.GetRequiredService<BlacklistEventHandler>().Subscribe();

                _modulesLoaded = true;
            }

            // ── 2. Register slash commands FIRST — không phụ thuộc DB ──
            // RegisterCommandsToGuildAsync(deleteMissing: true) đã overwrite toàn bộ
            // command set — KHÔNG cần DeleteApplicationCommandsAsync trước đó
            // (delete trước tạo ra window không có lệnh nào + thêm 1 REST call có thể fail)
#if DEBUG
            var guild = _client.GetGuild(_settings.TestGuildId);
            if (guild is null)
            {
                _logger.LogError(
                    "Test guild {GuildId} not found — kiểm tra TestGuildId hoặc bot chưa được invite",
                    _settings.TestGuildId);
            }
            else
            {
                await _interactions.RegisterCommandsToGuildAsync(_settings.TestGuildId, true);
                _logger.LogInformation(
                    "Commands registered to test guild {GuildId} — {Count} slash commands, app {AppId}",
                    _settings.TestGuildId, _interactions.SlashCommands.Count, _client.CurrentUser?.Id);
            }
#else
            await _interactions.RegisterCommandsGloballyAsync(true);
            _logger.LogInformation("Commands registered globally");
#endif

            // ── 3. DB-dependent work — cô lập để DB lỗi không giết command registration ──
            try
            {
                await _presenceSync.SyncOnReadyAsync(_client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Guild presence sync failed — bot vẫn hoạt động nhưng cần kiểm tra database connection");
            }

            _statusProvider.SetReady();
            _logger.LogInformation("Bot is ready!");

            if (!_startupNotificationSent)
            {
                _startupNotificationSent = true;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                        await SendStartupNotificationsAsync(_crashedLastTime, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send startup notifications");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "OnReadyAsync failed — slash commands có thể không được đăng ký");
        }
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        using var scope = _services.CreateScope();
        var autoRoleService = scope.ServiceProvider.GetRequiredService<IAutoRoleService>();

        var roleIds = (await autoRoleService.GetRoleIdsToApplyAsync(user.Guild.Id)).ToList();
        if (roleIds.Count == 0) return;

        try
        {
            await user.AddRolesAsync(roleIds);
            _logger.LogInformation("Applied {Count} auto roles to user {UserId} in guild {GuildId}",
                roleIds.Count, user.Id, user.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply auto roles to user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
    }

    private async Task OnButtonExecutedAsync(SocketMessageComponent interaction)
    {
        try
        {
            var customId = interaction.Data.CustomId;

            if (customId == VerifyInteractionService.VerifyButtonId)
            {
                var verifyService = _services.GetRequiredService<VerifyInteractionService>();
                await verifyService.HandleVerifyAsync(interaction);
                return;
            }

            if (customId == Dynamite.Modules.Giveaway.Helpers.GiveawayEmbedBuilder.EnterButtonId)
            {
                var giveawayService = _services.GetRequiredService<GiveawayInteractionService>();
                await giveawayService.HandleButtonAsync(interaction);
                return;
            }

            if (customId == Dynamite.Modules.Ticket.Helpers.TicketEmbedBuilder.OpenButtonId  ||
                customId == Dynamite.Modules.Ticket.Helpers.TicketEmbedBuilder.CloseButtonId ||
                customId == Dynamite.Modules.Ticket.Helpers.TicketEmbedBuilder.DeleteButtonId)
            {
                var ticketService = _services.GetRequiredService<TicketInteractionService>();
                await ticketService.HandleButtonAsync(interaction);
                return;
            }

            if (customId.StartsWith(RolePanelInteractionService.ButtonPrefix))
            {
                var rolePanelService = _services.GetRequiredService<RolePanelInteractionService>();
                await rolePanelService.HandleButtonAsync(interaction);
                return;
            }

            // Unhandled button
            _logger.LogWarning("Unhandled button executed with customId: {CustomId} by user {UserId}", customId, interaction.User.Id);
            if (!interaction.HasResponded)
                await interaction.RespondAsync("⚠️ Nút này không còn khả dụng hoặc không hợp lệ.", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling button interaction {CustomId}", interaction.Data.CustomId);
            if (!interaction.HasResponded)
                try { await interaction.RespondAsync("❌ Đã xảy ra lỗi khi xử lý thao tác này.", ephemeral: true); } catch {}
            else
                try { await interaction.FollowupAsync("❌ Đã xảy ra lỗi khi xử lý thao tác này.", ephemeral: true); } catch {}
        }
    }

    private async Task OnSelectMenuExecutedAsync(SocketMessageComponent interaction)
    {
        try
        {
            var customId = interaction.Data.CustomId;
            if (customId.StartsWith(RolePanelInteractionService.SelectPrefix))
            {
                var rolePanelService = _services.GetRequiredService<RolePanelInteractionService>();
                await rolePanelService.HandleSelectAsync(interaction);
                return;
            }

            // Unhandled select menu
            _logger.LogWarning("Unhandled select menu executed with customId: {CustomId} by user {UserId}", customId, interaction.User.Id);
            if (!interaction.HasResponded)
                await interaction.RespondAsync("⚠️ Menu này không còn khả dụng hoặc không hợp lệ.", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling select menu interaction {CustomId}", interaction.Data.CustomId);
            if (!interaction.HasResponded)
                try { await interaction.RespondAsync("❌ Đã xảy ra lỗi khi xử lý thao tác này.", ephemeral: true); } catch {}
            else
                try { await interaction.FollowupAsync("❌ Đã xảy ra lỗi khi xử lý thao tác này.", ephemeral: true); } catch {}
        }
    }

    private async Task OnModalSubmittedAsync(SocketModal modal)
    {
        // Cùng lý do: không dispose scope sớm — để InteractionService tự quản lý
        var ctx = new SocketInteractionContext(_client, modal);
        await _interactions.ExecuteCommandAsync(ctx, _services);
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        // ButtonExecuted / SelectMenuExecuted / ModalSubmitted đã có dedicated handlers
        if (interaction is SocketMessageComponent) return;
        if (interaction is SocketModal) return;

        // Race condition guard: interaction có thể đến trước OnReadyAsync hoàn thành
        // AddModulesAsync (tức là trước khi modules được load). Nếu chạy ExecuteCommandAsync
        // lúc này → UnknownCommand vì InteractionService chưa biết command nào cả.
        if (!_modulesLoaded)
        {
            try { await interaction.RespondAsync("⏳ Bot đang khởi động, vui lòng thử lại sau vài giây!", ephemeral: true); }
            catch { /* ignore — interaction có thể đã timeout */ }
            _logger.LogWarning("Interaction received before modules loaded — deferred for {Type}", interaction.GetType().Name);
            return;
        }

        try
        {
            // QUAN TRỌNG: pass root provider — InteractionService (AutoServiceScopes = true)
            // tự tạo scope theo vòng đời command. KHÔNG dùng `using var scope` ở đây:
            // RunMode.Async trả về ngay → scope bị dispose trong khi command còn đang chạy
            // → ObjectDisposedException bị nuốt → bot im lặng ("Ứng dụng không phản hồi")
            var ctx = new SocketInteractionContext(_client, interaction);
            var result = await _interactions.ExecuteCommandAsync(ctx, _services);
            if (!result.IsSuccess)
                _logger.LogError("ExecuteCommandAsync failed: {Error} — {Reason}", result.Error, result.ErrorReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnInteractionCreatedAsync threw for interaction type {Type}", interaction.GetType().Name);
        }
    }

    private Task OnInteractionExecutedAsync(
        ICommandInfo? command, IInteractionContext context, IResult result)
    {
        // Always log mod-level commands to the audit channel (success + failure).
        // ModAuditLogger filters by root command name — non-mod commands are skipped cheaply.
        _ = Task.Run(async () =>
        {
            try { await _modAuditLogger.LogAsync(command, context, result); }
            catch (Exception ex) { _logger.LogError(ex, "ModAuditLogger threw unexpectedly"); }
        });

        if (result.IsSuccess) return Task.CompletedTask;

        // UnmetPrecondition = user thiếu quyền — hành vi bình thường, không phải lỗi bot
        if (result.Error == InteractionCommandError.UnmetPrecondition)
        {
            _logger.LogInformation("Precondition blocked: {Reason} | Command: {Command}",
                result.ErrorReason, command?.Name ?? "unknown");
        }
        // UnknownCommand = stale Discord cache hoặc startup race — không phải lỗi nghiêm trọng
        else if (result.Error == InteractionCommandError.UnknownCommand)
        {
            _logger.LogWarning("UnknownCommand — stale Discord command registration? | Reason: {Reason}",
                result.ErrorReason);
        }
        else
        {
            // Cố gắng extract exception thật (có stack trace) nếu là ExecuteResult
            var innerException = result is ExecuteResult exec ? exec.Exception : null;
            _logger.LogError(innerException,
                "Interaction failed — Error: {Error} | Reason: {Reason} | Command: {Command}",
                result.Error, result.ErrorReason, command?.Name ?? "unknown");
        }

        // Respond to user với error message — bắt buộc để tránh "Ứng dụng không phản hồi"
        _ = Task.Run(async () =>
        {
            try
            {
                var errorMessage = result.Error switch
                {
                    InteractionCommandError.UnmetPrecondition => $"❌ {result.ErrorReason}",
                    InteractionCommandError.BadArgs           => "❌ Tham số không hợp lệ.",
                    InteractionCommandError.Exception         => "❌ Đã xảy ra lỗi nội bộ.",
                    InteractionCommandError.UnknownCommand    => "❌ Lệnh không tìm thấy. Thử reload Discord (Ctrl+R) rồi dùng lại.",
                    _                                         => $"❌ Lệnh thất bại: {result.ErrorReason}"
                };

                // Cố respond — nếu đã deferred thì dùng FollowupAsync, nếu chưa thì RespondAsync
                if (context.Interaction.HasResponded)
                    await context.Interaction.FollowupAsync(errorMessage, ephemeral: true);
                else
                    await context.Interaction.RespondAsync(errorMessage, ephemeral: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send error response to user");
            }

            // Gửi lên audit log nếu có
            try
            {
                if (context.Guild is null) return;
                using var scope = _services.CreateScope();
                var logService = scope.ServiceProvider.GetRequiredService<IServerLogService>();
                var auditChannelId = await logService.GetLogChannelAsync(context.Guild.Id, LogCategory.Audit);
                if (auditChannelId is null) return;

                var guild = _client.GetGuild(context.Guild.Id);
                var channel = guild?.GetTextChannel(auditChannelId.Value);
                if (channel is null) return;

                var embed = LogEmbedHelper.BotError(
                    command?.Name ?? "unknown",
                    result.Error?.ToString() ?? "Unknown",
                    result.ErrorReason);

                await channel.SendMessageAsync(embed: embed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send error to audit log");
            }
        });

        return Task.CompletedTask;
    }

    private Task LogAsync(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error    => LogLevel.Error,
            LogSeverity.Warning  => LogLevel.Warning,
            LogSeverity.Info     => LogLevel.Information,
            _ => LogLevel.Debug
        };
        _logger.Log(level, msg.Exception, "[Discord] {Message}", msg.Message);
        return Task.CompletedTask;
    }

}
