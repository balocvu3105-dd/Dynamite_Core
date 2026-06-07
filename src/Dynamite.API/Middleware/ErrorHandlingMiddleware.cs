// src/Dynamite.API/Middleware/ErrorHandlingMiddleware.cs
namespace Dynamite.API.Middleware;

using System.Net;
using System.Text.Json;

/// <summary>
/// Global error handler — bắt tất cả exception chưa được handle.
/// Trả về JSON response thay vì HTML error page mặc định của ASP.NET.
///
/// Tại sao cần middleware này?
/// → Controllers không nên try/catch mọi thứ — code sẽ rất lộn xộn.
/// → Một chỗ duy nhất handle tất cả error = dễ maintain.
/// → Trả về consistent JSON format cho mọi error.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized."),
            ArgumentException e => (HttpStatusCode.BadRequest, e.Message),
            KeyNotFoundException e => (HttpStatusCode.NotFound, e.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            error = message,
            status = (int)statusCode,
            path = context.Request.Path.Value
        });

        await context.Response.WriteAsync(body);
    }
}