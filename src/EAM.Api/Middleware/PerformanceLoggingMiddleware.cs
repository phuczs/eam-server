using System.Diagnostics;

namespace EAM.Api.Middleware;

/// <summary>Records request duration and surfaces it via response headers + slow-request warnings.</summary>
public class PerformanceLoggingMiddleware
{
    private const int SlowThresholdMs = 1000;
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();

        // OnStarting runs just before headers flush — the only point we can still add headers.
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers["X-Response-Time-Ms"] = sw.ElapsedMilliseconds.ToString();
            ctx.Response.Headers["X-Correlation-Id"] =
                ctx.Items["CorrelationId"]?.ToString() ?? ctx.TraceIdentifier;
            return Task.CompletedTask;
        });

        await _next(ctx);
        sw.Stop();

        var elapsed = sw.ElapsedMilliseconds;
        if (elapsed >= SlowThresholdMs)
            _logger.LogWarning("SLOW {Method} {Path} {Status} took {Ms}ms",
                ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, elapsed);
        else
            _logger.LogDebug("{Method} {Path} {Status} took {Ms}ms",
                ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, elapsed);
    }
}
