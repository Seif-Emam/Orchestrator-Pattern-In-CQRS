using System.Text.Json.Serialization;

namespace OrchestratorPattern.Api.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public string TraceId { get; init; } = string.Empty;

    public static ApiResponse<T> Ok(T data, string? traceId = null) => new()
    {
        Success = true,
        Data = data,
        Error = null,
        TraceId = traceId ?? string.Empty
    };

    public static ApiResponse<T> Fail(string code, string message, IEnumerable<string>? details = null, string? traceId = null) => new()
    {
        Success = false,
        Data = default,
        Error = new ApiError
        {
            Code = code,
            Message = message,
            Details = details?.ToList()
        },
        TraceId = traceId ?? string.Empty
    };

    public static ApiResponse<T> Fail(ApiError error, string? traceId = null) => new()
    {
        Success = false,
        Data = default,
        Error = error,
        TraceId = traceId ?? string.Empty
    };
}
