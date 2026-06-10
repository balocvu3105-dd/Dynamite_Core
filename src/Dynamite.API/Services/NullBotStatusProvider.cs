// src/Dynamite.API/Services/NullBotStatusProvider.cs
namespace Dynamite.API.Services;

using Dynamite.Shared;

/// <summary>
/// Fallback implementation của IBotStatusProvider cho môi trường
/// API chạy độc lập (không cùng process với bot).
///
/// Luôn trả về IsReady = false → BotHealthCheck sẽ là Degraded, không phải Unhealthy.
/// Đây là behavior đúng: API không biết bot đang ở đâu nên không thể khẳng định ready.
/// </summary>
internal sealed class NullBotStatusProvider : IBotStatusProvider
{
    public bool IsReady => false;
    public DateTime? LastReadyAt => null;
}