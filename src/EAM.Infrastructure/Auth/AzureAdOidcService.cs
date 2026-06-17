using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EAM.Infrastructure.Auth;

/// <summary>Microsoft Entra ID authorization-code OIDC client.</summary>
public sealed class AzureAdOidcService : IAzureAdOidcService
{
    private static readonly TimeSpan AuthSessionTtl = TimeSpan.FromMinutes(10);

    private readonly AzureAdOptions _opts;
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AzureAdOidcService> _logger;

    public AzureAdOidcService(
        IOptions<AzureAdOptions> opts,
        HttpClient http,
        IMemoryCache cache,
        ILogger<AzureAdOidcService> logger)
    {
        _opts = opts.Value;
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    private string Authority => $"{_opts.Instance.TrimEnd('/')}/{_opts.TenantId.Trim('/')}";
    private string AuthorizationEndpoint => $"{Authority}/oauth2/v2.0/authorize";
    private string TokenEndpoint => $"{Authority}/oauth2/v2.0/token";
    private string DiscoveryEndpoint => $"{Authority}/v2.0/.well-known/openid-configuration";

    public Task<AzureAdAuthorizationStart> BuildAuthorizationUrlAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        var sessionId = GenerateOpaqueValue();
        var state = GenerateOpaqueValue();
        var nonce = GenerateOpaqueValue();
        var codeVerifier = GenerateOpaqueValue();
        var codeChallenge = CreateCodeChallenge(codeVerifier);
        var scopes = EffectiveScopes();

        var query = string.Join("&",
            $"client_id={Uri.EscapeDataString(_opts.ClientId)}",
            "response_type=code",
            "response_mode=query",
            $"redirect_uri={Uri.EscapeDataString(_opts.RedirectUri)}",
            $"scope={Uri.EscapeDataString(scopes)}",
            $"state={Uri.EscapeDataString(state)}",
            $"nonce={Uri.EscapeDataString(nonce)}",
            $"code_challenge={Uri.EscapeDataString(codeChallenge)}",
            "code_challenge_method=S256");

        _cache.Set(CacheKey(sessionId), new AzureAdAuthSession(state, nonce, codeVerifier), AuthSessionTtl);

        return Task.FromResult(new AzureAdAuthorizationStart($"{AuthorizationEndpoint}?{query}", sessionId));
    }

    public async Task<string> ExchangeCodeForSubjectAsync(
        string code,
        string state,
        string sessionId,
        CancellationToken ct = default)
    {
        EnsureConfigured();

        if (!_cache.TryGetValue(CacheKey(sessionId), out AzureAdAuthSession? session) || session is null)
            throw new InvalidOperationException("Azure AD login session is missing or expired.");

        if (!FixedTimeEquals(session.State, state))
            throw new SecurityTokenException("Azure AD callback state does not match the login session.");

        var form = new Dictionary<string, string>
        {
            ["client_id"] = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _opts.RedirectUri,
            ["code_verifier"] = session.CodeVerifier,
            ["scope"] = EffectiveScopes()
        };

        using var response = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Azure AD token endpoint returned {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Azure AD token exchange failed ({(int)response.StatusCode}): {body}");
        }

        _cache.Remove(CacheKey(sessionId));

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenEndpointResponse>(ct)
            ?? throw new InvalidOperationException("Empty Azure AD token response.");

        return await ValidateIdTokenAsync(tokenResponse.IdToken, session.Nonce, ct);
    }

    private async Task<string> ValidateIdTokenAsync(string idToken, string expectedNonce, CancellationToken ct)
    {
        var discovery = await _http.GetFromJsonAsync<OpenIdConfigurationResponse>(DiscoveryEndpoint, ct)
            ?? throw new InvalidOperationException("Empty Azure AD discovery response.");

        var jwks = await _http.GetStringAsync(discovery.JwksUri, ct);
        var signingKeys = new JsonWebKeySet(jwks).GetSigningKeys();

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var validationParams = new TokenValidationParameters
        {
            ValidIssuer = discovery.Issuer,
            ValidAudience = _opts.ClientId,
            IssuerSigningKeys = signingKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var principal = handler.ValidateToken(idToken, validationParams, out _);
        var nonce = principal.FindFirst(JwtRegisteredClaimNames.Nonce)?.Value
                    ?? throw new SecurityTokenException("id_token is missing the 'nonce' claim.");

        if (!FixedTimeEquals(expectedNonce, nonce))
            throw new SecurityTokenException("id_token nonce does not match the Azure AD login session.");

        return principal.FindFirst("oid")?.Value
               ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
               ?? throw new SecurityTokenException("id_token is missing both 'oid' and 'sub' claims.");
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_opts.TenantId)
            || string.IsNullOrWhiteSpace(_opts.ClientId)
            || string.IsNullOrWhiteSpace(_opts.ClientSecret)
            || string.IsNullOrWhiteSpace(_opts.RedirectUri))
        {
            throw new InvalidOperationException(
                "AzureAd:TenantId, AzureAd:ClientId, AzureAd:ClientSecret, and AzureAd:RedirectUri are required.");
        }
    }

    private string EffectiveScopes()
        => string.Join(" ", _opts.Scopes.Length > 0 ? _opts.Scopes : ["openid", "profile", "email"]);

    private static string GenerateOpaqueValue()
        => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static string CreateCodeChallenge(string codeVerifier)
        => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string CacheKey(string sessionId) => $"azure-ad:v1:{sessionId}";

    private sealed record AzureAdAuthSession(string State, string Nonce, string CodeVerifier);

    private sealed record TokenEndpointResponse(
        [property: JsonPropertyName("id_token")] string IdToken,
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn);

    private sealed record OpenIdConfigurationResponse(
        [property: JsonPropertyName("issuer")] string Issuer,
        [property: JsonPropertyName("jwks_uri")] string JwksUri);
}
