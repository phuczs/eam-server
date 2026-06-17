using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EAM.Application.DTOs.Auth.Request;
using EAM.Application.DTOs.Auth.Response;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Services;
using EAM.Application.Options;
using Microsoft.Extensions.Options;

namespace EAM.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string SingpassSessionCookie = "eam_singpass_session";
    private const string AzureAdSessionCookie = "eam_aad_session";
    private const string RefreshTokenCookie = "eam_refresh_token";

    private readonly IAuthService _auth;
    private readonly ISingpassOidcService _singpass;
    private readonly IAzureAdOidcService _azureAd;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        IAuthService auth,
        ISingpassOidcService singpass,
        IAzureAdOidcService azureAd,
        IOptions<JwtOptions> jwtOptions)
    {
        _auth = auth;
        _singpass = singpass;
        _azureAd = azureAd;
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>
    /// Creates a backend-owned Singpass login session and returns the authorization URL
    /// the SPA should redirect the browser to.
    /// </summary>
    [HttpGet("singpass/init")]
    [AllowAnonymous]
    public async Task<IActionResult> InitiateSingpass(CancellationToken ct)
    {
        var start = await _singpass.BuildAuthorizationUrlAsync(ct);

        SetSingpassSessionCookie(start.SessionId);

        return Ok(new { url = start.Url });
    }

    /// <summary>
    /// Creates a backend-owned Singpass login session and redirects the browser
    /// directly to Singpass/MockPass.
    /// </summary>
    [HttpGet("singpass/login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithSingpass(CancellationToken ct)
    {
        var start = await _singpass.BuildAuthorizationUrlAsync(ct);
        SetSingpassSessionCookie(start.SessionId);
        return Redirect(start.Url);
    }

    /// <summary>
    /// Completes the Singpass login: exchanges the authorization code, sets the
    /// refresh token cookie, and returns EAM's own access token.
    /// </summary>
    [HttpPost("singpass/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> SingpassCallback(
        [FromBody] SingpassCallbackRequest request,
        CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(SingpassSessionCookie, out var sessionId)
            || string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest("Singpass login session cookie is missing or expired.");
        }

        var result = await _auth.ExternalLoginAsync("Singpass", request.Code, request.State, sessionId, ct);
        SetRefreshTokenCookie(result.RefreshToken);
        Response.Cookies.Delete(SingpassSessionCookie, new CookieOptions
        {
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/auth/singpass"
        });
        return Ok(ToTokenResponse(result));
    }

    private void SetSingpassSessionCookie(string sessionId)
    {
        Response.Cookies.Append(SingpassSessionCookie, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/auth/singpass",
            MaxAge = TimeSpan.FromMinutes(5),
            IsEssential = true
        });
    }

    /// <summary>
    /// Creates a backend-owned Azure AD login session and returns the authorization URL
    /// the SPA should redirect the browser to.
    /// </summary>
    [HttpGet("azure-ad/init")]
    [AllowAnonymous]
    public async Task<IActionResult> InitiateAzureAd(CancellationToken ct)
    {
        var start = await _azureAd.BuildAuthorizationUrlAsync(ct);

        Response.Cookies.Append(AzureAdSessionCookie, start.SessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/auth/azure-ad",
            MaxAge = TimeSpan.FromMinutes(10),
            IsEssential = true
        });

        return Ok(new { url = start.Url });
    }

    /// <summary>
    /// Completes the Azure AD login, sets the refresh token cookie, and returns
    /// EAM's own access token.
    /// </summary>
    [HttpPost("azure-ad/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> AzureAdCallback(
        [FromBody] AzureAdCallbackRequest request,
        CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(AzureAdSessionCookie, out var sessionId)
            || string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest("Azure AD login session cookie is missing or expired.");
        }

        var result = await _auth.ExternalLoginAsync("AzureAd", request.Code, request.State, sessionId, ct);
        SetRefreshTokenCookie(result.RefreshToken);
        Response.Cookies.Delete(AzureAdSessionCookie, new CookieOptions
        {
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/auth/azure-ad"
        });

        return Ok(ToTokenResponse(result));
    }

    /// <summary>
    /// Rotates the HttpOnly refresh token cookie and returns a new access token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized("Refresh token cookie is missing or expired.");
        }

        var result = await _auth.RefreshAsync(refreshToken, ct);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ToTokenResponse(result));
    }

    /// <summary>Revokes all refresh tokens for the authenticated user.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        await _auth.LogoutAsync(userId, ct);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    /// <summary>
    /// Returns this RP's public JWKS so MockPass (or real Singpass) can verify
    /// client_assertion JWTs and encrypt id_tokens. Point MockPass env var
    /// FAPI_CLIENT_JWKS_ENDPOINT at this URL for the v3 FAPI flow.
    /// </summary>
    [HttpGet("jwks")]
    [AllowAnonymous]
    public IActionResult GetJwks()
    {
        var json = _singpass.GetPublicKeyJwks();
        return Content(json, "application/json");
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(_jwtOptions.RefreshTokenDays),
            IsEssential = true
        });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/auth"
        });
    }

    private static AuthTokenResponse ToTokenResponse(AuthResult result)
        => new(result.AccessToken, result.ExpiresAtUtc);
}
