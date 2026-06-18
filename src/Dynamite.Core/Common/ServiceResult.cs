// src/Dynamite.Core/Common/ServiceResult.cs
namespace Dynamite.Core.Common;

/// <summary>
/// Kết quả của một operation không trả dữ liệu.
/// Thay thế raw tuple (bool success, string message).
/// </summary>
public class ServiceResult
{
    public bool   Success      { get; protected init; }
    public string ErrorMessage { get; protected init; } = string.Empty;

    public static ServiceResult Ok()               => new() { Success = true };
    public static ServiceResult Fail(string error) => new() { Success = false, ErrorMessage = error };

    /// <summary>Implicit bool — dùng được trong if (result) { ... }</summary>
    public static implicit operator bool(ServiceResult r) => r.Success;
}

/// <summary>
/// Kết quả của một operation trả dữ liệu.
/// Thay thế raw tuple (bool success, string message, T value, ...).
/// </summary>
public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    // BUG FIX: phải set Success = true, Value = value cả hai
    public static ServiceResult<T> Ok(T value)          => new() { Success = true, Value = value };
    public static new ServiceResult<T> Fail(string error) => new() { Success = false, ErrorMessage = error };

    /// <summary>
    /// Deconstruct để backward-compatible với code cũ:
    /// var (ok, err, value) = result;
    /// </summary>
    public void Deconstruct(out bool success, out string error, out T? value)
    {
        success = Success;
        error   = ErrorMessage;
        value   = Value;
    }
}
