// src/Dynamite.API/Filters/RequireGuildAdminAttribute.cs
namespace Dynamite.API.Filters;

using Dynamite.API.Auth;
using Dynamite.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Action filter này đảm bảo request chỉ được xử lý nếu user có quyền ManageGuild.
/// Yêu cầu:
/// 1. Route chứa tham số {guildId}.
/// 2. Header chứa X-Discord-Token.
/// 3. Token hợp lệ và có quyền ManageGuild.
/// </summary>
public class RequireGuildAdminAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Trích xuất guildId từ route values
        if (!context.RouteData.Values.TryGetValue("guildId", out var guildIdObj) || 
            guildIdObj is not string guildId || string.IsNullOrWhiteSpace(guildId))
        {
            context.Result = new BadRequestObjectResult(new { error = "Missing guildId in route parameters." });
            return;
        }

        // Trích xuất X-Discord-Token từ header
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Discord-Token", out var tokenValues) || 
            string.IsNullOrEmpty(tokenValues.FirstOrDefault()))
        {
            context.Result = new BadRequestObjectResult(new { error = "X-Discord-Token header is required for this action." });
            return;
        }
        var discordToken = tokenValues.First();

        // Resolve các services cần thiết (vì Attribute không inject constructor trực tiếp được)
        var discordService = context.HttpContext.RequestServices.GetRequiredService<DiscordOAuthService>();
        var guildAuthService = context.HttpContext.RequestServices.GetRequiredService<GuildAuthorizationService>();

        try
        {
            // Lấy danh sách guild (có hỗ trợ cache bên trong DiscordOAuthService hoặc GuildAuthorizationService)
            var guilds = await discordService.GetManageableGuildsAsync(discordToken, context.HttpContext.RequestAborted);
            
            // Kiểm tra user có quyền quản lý guild này hay không
            var targetGuild = guildAuthService.GetManageableGuild(guilds, guildId);
            
            if (targetGuild is null)
            {
                context.Result = new StatusCodeResult(403); // Forbidden
                return;
            }

            // Đưa thông tin guild vào HttpContext.Items để Controller có thể tái sử dụng nếu cần
            context.HttpContext.Items["GuildContext"] = targetGuild;
        }
        catch (Exception)
        {
            context.Result = new StatusCodeResult(403); // Forbidden nếu gọi Discord API lỗi (thường do token hết hạn)
            return;
        }

        // Cho phép request đi tiếp vào Controller
        await next();
    }
}
