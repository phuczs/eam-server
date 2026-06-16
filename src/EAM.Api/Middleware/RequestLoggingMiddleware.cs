namespace EAM.Api.Middleware;

/// <summary>
/// Logs inbound requests and their outcome. Optimised from the reference version:
/// one structured line in/out (not one log per field), headers only at Debug, and
/// sensitive headers (Authorization/Cookie) are redacted so secrets never hit the log.
/// </summary>
public class RequestLoggingMiddleware
{
    private static readonly string[] SensitiveHeaders = { "Authorization", "Cookie", "Set-Cookie" };

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var req = context.Request;
        var cid = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

        _logger.LogInformation("--> {Method} {Path}{Query} [cid:{Cid}] host={Host} type={ContentType}",
            req.Method, req.Path, req.QueryString, cid, req.Host, req.ContentType);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var header in req.Headers)
            {
                var value = SensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase)
                    ? "***redacted***"
                    : header.Value.ToString();
                _logger.LogDebug("    header {Key}={Value}", header.Key, value);
            }
        }

        await _next(context);

        _logger.LogInformation("<-- {Status} {Method} {Path} [cid:{Cid}]",
            context.Response.StatusCode, req.Method, req.Path, cid);
    }
}
