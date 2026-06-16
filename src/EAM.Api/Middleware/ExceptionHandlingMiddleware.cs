using System.Net;
using System.Text.Json;
using EAM.Application.Common;

namespace EAM.Api.Middleware;

/// <summary>
/// Global exception handler. Produces the standardised error envelope and never leaks
/// stack traces to clients. (Add a FluentValidation ValidationException catch here once
/// validators are introduced.)
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (AppException aex)
        {
            await WriteAsync(ctx, aex.StatusCode, aex.Code, aex.Message,
                aex.Errors is null ? null : new Dictionary<string, string[]>(aex.Errors));
        }
        catch (Exception ex)
        {
            var cid = ctx.Items["CorrelationId"]?.ToString();
            _logger.LogError(ex, "Unhandled exception [cid:{Cid}]", cid);
            await WriteAsync(ctx, HttpStatusCode.InternalServerError, "server_error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteAsync(HttpContext ctx, HttpStatusCode status, string code,
        string message, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.Clear();
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json";

        var body = new ErrorResponse
        {
            Code = code,
            Message = message,
            TraceId = ctx.Items["CorrelationId"]?.ToString(),
            Errors = errors
        };

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
