using System.Net;

namespace EAM.Application.Common;

/// <summary>
/// Domain/application error carrying an HTTP status. The exception-handling
/// middleware translates this into the standardised error envelope.
/// </summary>
public class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string Code { get; }

    /// <summary>Field-level validation errors (field name -> messages).</summary>
    public IDictionary<string, string[]>? Errors { get; }

    public AppException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        string code = "app_error",
        IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Errors = errors;
    }

    // ── 400 ──────────────────────────────────────────────────────────────────
    public static AppException BadRequest(string message, string code = "bad_request") =>
        new(message, HttpStatusCode.BadRequest, code);

    public static AppException ValidationError(IDictionary<string, string[]> errors, string message = "One or more validation failures have occurred.") =>
        new(message, HttpStatusCode.BadRequest, "validation_error", errors);

    public static AppException ValidationError(string field, string error) =>
        new($"Validation failed for field '{field}': {error}", HttpStatusCode.BadRequest, "validation_error",
            new Dictionary<string, string[]> { { field, new[] { error } } });

    // ── 401 / 403 ──────────────────────────────────────────────────────────────
    public static AppException Unauthorized(string message = "Authentication required.") =>
        new(message, HttpStatusCode.Unauthorized, "unauthorized");

    public static AppException Forbidden(string message = "You do not have access to this resource.") =>
        new(message, HttpStatusCode.Forbidden, "forbidden");

    // ── 404 ──────────────────────────────────────────────────────────────────
    public static AppException NotFound(string what) =>
        new($"{what} was not found.", HttpStatusCode.NotFound, "not_found");

    public static AppException NotFound(string what, object key) =>
        new($"{what} with ID '{key}' was not found.", HttpStatusCode.NotFound, "not_found");

    // ── 409 ──────────────────────────────────────────────────────────────────
    public static AppException Conflict(string message) =>
        new(message, HttpStatusCode.Conflict, "conflict");

    public static AppException ConcurrencyConflict(string message = "The resource was modified by another process. Please retry.") =>
        new(message, HttpStatusCode.Conflict, "concurrency_conflict");

    // ── 429 ──────────────────────────────────────────────────────────────────
    public static AppException ResourceLocked(string resourceName) =>
        new($"The resource '{resourceName}' is currently locked. Please try again later.", HttpStatusCode.TooManyRequests, "resource_locked");
}
