namespace EAM.Application.Common;

/// <summary>Uniform success/error envelope returned by the API.</summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ErrorResponse? Error { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string? traceId = null)
        => new() { Success = true, Data = data, TraceId = traceId };

    public static ApiResponse<T> Fail(ErrorResponse error, string? traceId = null)
        => new() { Success = false, Error = error, TraceId = traceId ?? error.TraceId };
}
