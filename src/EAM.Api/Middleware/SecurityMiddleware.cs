namespace EAM.Api.Middleware;

/// <summary>
/// Coarse role gate for management endpoints. Controller-level [Authorize(Roles=...)]
/// remains the primary guard; this is a defence-in-depth net for admin route prefixes.
/// </summary>
public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;

    // Endpoints that require management (admin) authority.
    private static readonly string[] AdminPrefixes =
    {
        "/api/users",
        "/api/topups",
        "/api/courses",
        "/api/billing",
        "/api/invoices",
        "/api/payments",
        "/api/fas",
        "/api/audit"
    };

    private static readonly string[] AdminRoles =
    {
        "ServiceAdmin", "TenantUserAdmin", "ServiceSupportAdmin"
    };

    public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        var needsAdmin = AdminPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (needsAdmin)
        {
            if (ctx.User?.Identity?.IsAuthenticated != true)
            {
                await WriteProblemAsync(ctx, StatusCodes.Status401Unauthorized,
                    "unauthorized", "Authentication is required.");
                return;
            }

            if (!AdminRoles.Any(r => ctx.User.IsInRole(r)))
            {
                _logger.LogWarning("Role gate blocked on {Path}.", path);
                await WriteProblemAsync(ctx, StatusCodes.Status403Forbidden,
                    "forbidden", "You do not have permission to access this resource.");
                return;
            }
        }

        await _next(ctx);
    }

    private static Task WriteProblemAsync(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsJsonAsync(new { code, message, traceId = ctx.TraceIdentifier });
    }
}
