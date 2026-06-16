namespace EAM.Application.Common;

/// <summary>Structured error payload carried by <see cref="ApiResponse{T}"/>.</summary>
public class ErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? TraceId { get; init; }

    /// <summary>Optional field-level validation errors (field -> messages).</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
