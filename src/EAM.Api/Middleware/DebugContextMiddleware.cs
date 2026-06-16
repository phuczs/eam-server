using System.Diagnostics;

namespace EAM.Api.Middleware;

/// <summary>Attaches a correlation id to every request (read from header or generated).</summary>
public class DebugContextMiddleware
{
    public const string CorrelationHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public DebugContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers.TryGetValue(CorrelationHeader, out var h) && !string.IsNullOrEmpty(h)
            ? h.ToString()
            : Activity.Current?.Id ?? Guid.NewGuid().ToString();

        ctx.Items["CorrelationId"] = correlationId;
        ctx.Response.Headers[CorrelationHeader] = correlationId;
        await _next(ctx);
    }
}
