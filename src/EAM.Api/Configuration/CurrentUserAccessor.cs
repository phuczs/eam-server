using System.Security.Claims;
using EAM.Application.Common;
using EAM.Application.Interfaces.Infrastructures;

namespace EAM.Api.Configuration;

/// <summary>Resolves the authenticated user from HttpContext claims.</summary>
public class HttpCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;
    public HttpCurrentUserAccessor(IHttpContextAccessor http) => _http = http;

    public CurrentUser? Current
    {
        get
        {
            var principal = _http.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true) return null;

            var sub = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out var userId)) return null;

            return new CurrentUser
            {
                UserId = userId,
                Email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            };
        }
    }

    public CurrentUser Require() => Current ?? throw AppException.Unauthorized();
}

/// <summary>Exposes the client IP for auditing.</summary>
public class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _http;
    public HttpRequestContext(IHttpContextAccessor http) => _http = http;
    public string? SourceIp => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
