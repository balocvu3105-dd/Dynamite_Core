// src/Dynamite.Shared/IBotStatusProvider.cs
namespace Dynamite.Shared;

/// <summary>
/// Abstraction cho phép Dynamite.API đọc trạng thái bot
/// mà không cần reference Dynamite.Bot hay Discord.Net.
///
/// Bot project implement và register as Singleton.
/// API project chỉ cần inject interface này.
/// </summary>
public interface IBotStatusProvider
{
    /// <summary>Bot đã connected và ready chưa.</summary>
    bool IsReady { get; }

    /// <summary>Thời điểm bot ready lần cuối (UTC). Null nếu chưa ready lần nào.</summary>
    DateTime? LastReadyAt { get; }
}