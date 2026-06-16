using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using EAM.Application.Common;

namespace EAM.Api.Configuration;

/// <summary>
/// Wraps successful action results in the unified <see cref="ApiResponse{T}"/> envelope so
/// every JSON response shares one shape without each controller building it. Errors are
/// handled by ExceptionHandlingMiddleware, so this only touches the success path.
///
/// Deliberately skipped: FileResult (binary downloads stream raw), already-wrapped
/// payloads (no double-wrapping), and non-2xx results (they carry their own shape).
/// </summary>
public class ApiResponseWrapperFilter : IResultFilter
{
    public void OnResultExecuted(ResultExecutedContext context) { }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        var traceId = context.HttpContext.Items["CorrelationId"]?.ToString();

        switch (context.Result)
        {
            case ObjectResult obj:
            {
                var status = obj.StatusCode ?? StatusCodes.Status200OK;
                if (status is < 200 or >= 300) return;
                if (obj.Value is null) { obj.Value = WrapNull(traceId); obj.DeclaredType = typeof(ApiResponse<object>); return; }
                if (IsAlreadyWrapped(obj.Value.GetType())) return;

                obj.Value = Wrap(obj.Value, traceId);
                obj.DeclaredType = obj.Value.GetType();
                return;
            }
            case EmptyResult:
                context.Result = new ObjectResult(WrapNull(traceId)) { StatusCode = StatusCodes.Status200OK };
                return;
            case StatusCodeResult sc when sc.StatusCode is StatusCodes.Status204NoContent:
                context.Result = new ObjectResult(WrapNull(traceId)) { StatusCode = StatusCodes.Status200OK };
                return;
            // FileResult, ChallengeResult, RedirectResult, etc. pass through untouched.
        }
    }

    public static object Wrap(object value, string? traceId)
    {
        var responseType = typeof(ApiResponse<>).MakeGenericType(value.GetType());
        var response = Activator.CreateInstance(responseType)!;

        responseType.GetProperty(nameof(ApiResponse<object>.Success))!.SetValue(response, true);
        responseType.GetProperty(nameof(ApiResponse<object>.Data))!.SetValue(response, value);
        responseType.GetProperty(nameof(ApiResponse<object>.TraceId))!.SetValue(response, traceId);

        return response;
    }

    private static ApiResponse<object?> WrapNull(string? traceId)
        => new() { Success = true, Data = null, TraceId = traceId };

    private static bool IsAlreadyWrapped(Type t)
        => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ApiResponse<>);
}
