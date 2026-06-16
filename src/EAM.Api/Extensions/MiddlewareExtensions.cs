using EAM.Api.Middleware;

namespace EAM.Api.Extensions;

/// <summary>
/// Pipeline registration helpers. Individual <c>Use*</c> methods keep Program.cs readable;
/// <see cref="UseEamObservability"/> bundles the cross-cutting trio + exception handler in
/// the correct order.
/// </summary>
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceLogging(this IApplicationBuilder app)
        => app.UseMiddleware<PerformanceLoggingMiddleware>();

    public static IApplicationBuilder UseDebugContext(this IApplicationBuilder app)
        => app.UseMiddleware<DebugContextMiddleware>();

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();

    public static IApplicationBuilder UseCustomExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();

    public static IApplicationBuilder UseSecurity(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityMiddleware>();

    /// <summary>
    /// Registers (in order): performance timing → correlation id → request logging →
    /// global exception handling. Call this before authentication.
    /// </summary>
    public static IApplicationBuilder UseEamObservability(this IApplicationBuilder app)
    {
        app.UsePerformanceLogging();
        app.UseDebugContext();
        app.UseRequestLogging();
        app.UseCustomExceptionHandling();
        return app;
    }
}
