using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Options;
using Jose;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EAM.Infrastructure.Auth;

/// <summary>
/// Singpass v3 FAPI relying-party implementation. In Mockpass mode all requests
/// go to the local/Azure MockPass emulator's /singpass/v3/fapi endpoints.
/// </summary>
public sealed class SingpassOidcService : ISingpassOidcService
{
    private static readonly TimeSpan AuthSessionTtl = TimeSpan.FromMinutes(5);
    private const string ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    private readonly SingpassOptions _opts;
    private readonly HttpClient _http;
    private readonly IRpKeyStore _keyStore;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SingpassOidcService> _logger;

    public SingpassOidcService(
        IOptions<SingpassOptions> opts,
        HttpClient http,
        IRpKeyStore keyStore,
        IMemoryCache cache,
        ILogger<SingpassOidcService> logger)
    {
        _opts = opts.Value;
        _http = http;
        _keyStore = keyStore;
        _cache = cache;
        _logger = logger;
    }

    private bool IsMockpass => string.Equals(_opts.Mode, "Mockpass", StringComparison.OrdinalIgnoreCase);

    private string MockpassBaseUrl => _opts.Mockpass.BaseUrl.TrimEnd('/');

    private string Issuer
    {
        get
        {
            if (!IsMockpass)
                return _opts.Issuer.TrimEnd('/');

            var configured = string.IsNullOrWhiteSpace(_opts.Mockpass.Issuer)
                ? MockpassBaseUrl
                : _opts.Mockpass.Issuer.TrimEnd('/');

            return configured.EndsWith("/v3/fapi", StringComparison.OrdinalIgnoreCase)
                ? configured
                : $"{configured}/singpass/v3/fapi";
        }
    }

    private string ParEndpoint => $"{Issuer}/par";
    private string AuthEndpoint => $"{Issuer}/auth";
    private string TokenEndpoint => $"{Issuer}/token";
    private string JwksEndpoint => $"{Issuer}/.well-known/keys";

    public async Task<SingpassAuthorizationStart> BuildAuthorizationUrlAsync(CancellationToken ct = default)
    {
        var sessionId = GenerateOpaqueValue();
        var state = GenerateOpaqueValue();
        var nonce = GenerateOpaqueValue();
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = CreateCodeChallenge(codeVerifier);
        var dpopKey = GenerateDpopKey();

        var form = new Dictionary<string, string>
        {
            ["client_id"] = _opts.ClientId,
            ["redirect_uri"] = _opts.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", _opts.Scopes.Length > 0 ? _opts.Scopes : ["openid"]),
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["client_assertion_type"] = ClientAssertionType,
            ["client_assertion"] = BuildClientAssertion()
        };

        AddIfConfigured(form, "acr_values", _opts.AcrValues);
        AddIfConfigured(form, "redirect_uri_https_type", _opts.RedirectUriHttpsType);
        AddIfConfigured(form, "authentication_context_type", _opts.AuthenticationContextType);
        AddIfConfigured(form, "authentication_context_message", _opts.AuthenticationContextMessage);

        using var request = new HttpRequestMessage(HttpMethod.Post, ParEndpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.TryAddWithoutValidation("DPoP", BuildDpopProof(ParEndpoint, dpopKey));

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Singpass PAR endpoint returned {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Singpass PAR request failed ({(int)response.StatusCode}): {body}");
        }

        var par = await response.Content.ReadFromJsonAsync<PushedAuthorizationResponse>(ct)
            ?? throw new InvalidOperationException("Empty Singpass PAR response.");

        _cache.Set(
            CacheKey(sessionId),
            new SingpassAuthSession(state, nonce, codeVerifier, dpopKey),
            AuthSessionTtl);

        var url = $"{AuthEndpoint}?client_id={Uri.EscapeDataString(_opts.ClientId)}&request_uri={Uri.EscapeDataString(par.RequestUri)}";
        return new SingpassAuthorizationStart(url, sessionId);
    }

    public async Task<string> ExchangeCodeForSubjectAsync(
        string code,
        string state,
        string sessionId,
        CancellationToken ct = default)
    {
        if (!_cache.TryGetValue(CacheKey(sessionId), out SingpassAuthSession? session) || session is null)
            throw new InvalidOperationException("Singpass login session is missing or expired.");

        if (!FixedTimeEquals(session.State, state))
            throw new SecurityTokenException("Singpass callback state does not match the login session.");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _opts.RedirectUri,
            ["client_id"] = _opts.ClientId,
            ["code_verifier"] = session.CodeVerifier,
            ["client_assertion_type"] = ClientAssertionType,
            ["client_assertion"] = BuildClientAssertion()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.TryAddWithoutValidation("DPoP", BuildDpopProof(TokenEndpoint, session.DpopKey));

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Singpass token endpoint returned {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Token exchange failed ({(int)response.StatusCode}): {body}");
        }

        _cache.Remove(CacheKey(sessionId));

        var tokenResp = await response.Content.ReadFromJsonAsync<TokenEndpointResponse>(ct)
            ?? throw new InvalidOperationException("Empty token response from Singpass.");

        var signedIdToken = DecryptIdToken(tokenResp.IdToken);
        return await ValidateIdTokenAsync(signedIdToken, session.Nonce, ct);
    }

    public string GetPublicKeyJwks()
    {
        var signing = _keyStore.SigningKey.ExportParameters(includePrivateParameters: false);
        var encryption = _keyStore.EncryptionKey.ExportParameters(includePrivateParameters: false);

        var keys = new[]
        {
            new Dictionary<string, string>
            {
                ["kty"] = "EC",
                ["crv"] = "P-256",
                ["x"] = Base64UrlEncoder.Encode(signing.Q.X!),
                ["y"] = Base64UrlEncoder.Encode(signing.Q.Y!),
                ["kid"] = _keyStore.SigningKeyId,
                ["use"] = "sig",
                ["alg"] = "ES256"
            },
            new Dictionary<string, string>
            {
                ["kty"] = "EC",
                ["crv"] = "P-256",
                ["x"] = Base64UrlEncoder.Encode(encryption.Q.X!),
                ["y"] = Base64UrlEncoder.Encode(encryption.Q.Y!),
                ["kid"] = _keyStore.EncryptionKeyId,
                ["use"] = "enc",
                ["alg"] = "ECDH-ES+A256KW"
            }
        };

        return JsonSerializer.Serialize(new { keys });
    }

    private string BuildClientAssertion()
    {
        var now = DateTimeOffset.UtcNow;
        using var signingKey = ECDsa.Create(_keyStore.SigningKey.ExportParameters(includePrivateParameters: true));
        var key = new ECDsaSecurityKey(signingKey)
        {
            KeyId = _keyStore.SigningKeyId,
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
        var creds = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);
        var header = new JwtHeader(creds)
        {
            ["typ"] = "JWT",
            ["kid"] = _keyStore.SigningKeyId
        };
        var payload = new JwtPayload
        {
            [JwtRegisteredClaimNames.Iss] = _opts.ClientId,
            [JwtRegisteredClaimNames.Sub] = _opts.ClientId,
            [JwtRegisteredClaimNames.Aud] = Issuer,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            [JwtRegisteredClaimNames.Iat] = now.ToUnixTimeSeconds(),
            [JwtRegisteredClaimNames.Exp] = now.AddMinutes(2).ToUnixTimeSeconds()
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
    }

    private string BuildDpopProof(string endpoint, DpopKeyMaterial material)
    {
        var now = DateTimeOffset.UtcNow;
        using var dpopKey = CreateDpopKey(material);
        var key = new ECDsaSecurityKey(dpopKey)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
        var creds = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);
        var header = new JwtHeader(creds)
        {
            ["typ"] = "dpop+jwt",
            ["jwk"] = new Dictionary<string, string>
            {
                ["kty"] = "EC",
                ["crv"] = "P-256",
                ["x"] = material.X,
                ["y"] = material.Y
            }
        };
        var payload = new JwtPayload
        {
            ["htu"] = endpoint,
            ["htm"] = "POST",
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            [JwtRegisteredClaimNames.Iat] = now.ToUnixTimeSeconds(),
            [JwtRegisteredClaimNames.Exp] = now.AddMinutes(2).ToUnixTimeSeconds(),
            ["nonce"] = Guid.NewGuid().ToString("N")
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
    }

    private string DecryptIdToken(string encryptedIdToken)
    {
        var enc = _keyStore.EncryptionKey.ExportParameters(includePrivateParameters: true);
        var jwk = new Jwk(
            crv: "P-256",
            x: Base64UrlEncoder.Encode(enc.Q.X!),
            y: Base64UrlEncoder.Encode(enc.Q.Y!),
            d: Base64UrlEncoder.Encode(enc.D!));

        return JWT.Decode(encryptedIdToken, jwk, JweAlgorithm.ECDH_ES_A256KW, JweEncryption.A256GCM);
    }

    private async Task<string> ValidateIdTokenAsync(string signedIdToken, string expectedNonce, CancellationToken ct)
    {
        var signingKeys = await FetchSigningKeysAsync(ct);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var validationParams = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = _opts.ClientId,
            IssuerSigningKeys = signingKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(60),
        };

        var principal = handler.ValidateToken(signedIdToken, validationParams, out _);
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? throw new SecurityTokenException("id_token is missing the 'sub' claim.");
        var nonce = principal.FindFirst(JwtRegisteredClaimNames.Nonce)?.Value
                    ?? throw new SecurityTokenException("id_token is missing the 'nonce' claim.");

        if (!FixedTimeEquals(expectedNonce, nonce))
            throw new SecurityTokenException("id_token nonce does not match the login session.");

        var identityNumber = TryReadIdentityNumber(signedIdToken);
        var providerSubject = string.IsNullOrWhiteSpace(identityNumber) ? sub : identityNumber;

        _logger.LogInformation("Singpass login validated for sub={Sub}", sub);
        return providerSubject;
    }

    private async Task<IEnumerable<SecurityKey>> FetchSigningKeysAsync(CancellationToken ct)
    {
        var json = await _http.GetStringAsync(JwksEndpoint, ct);
        return new JsonWebKeySet(json).GetSigningKeys();
    }

    private static string? TryReadIdentityNumber(string signedIdToken)
    {
        var parts = signedIdToken.Split('.');
        if (parts.Length < 2)
            return null;

        var payloadJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);

        return doc.RootElement.TryGetProperty("sub_attributes", out var subAttributes)
               && subAttributes.TryGetProperty("identity_number", out var identityNumber)
            ? identityNumber.GetString()
            : null;
    }

    private static string GenerateOpaqueValue()
        => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static string GenerateCodeVerifier()
        => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static string CreateCodeChallenge(string codeVerifier)
        => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

    private static DpopKeyMaterial GenerateDpopKey()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(includePrivateParameters: true);
        return new DpopKeyMaterial(
            Base64UrlEncoder.Encode(parameters.Q.X!),
            Base64UrlEncoder.Encode(parameters.Q.Y!),
            Base64UrlEncoder.Encode(parameters.D!));
    }

    private static ECDsa CreateDpopKey(DpopKeyMaterial material)
    {
        return ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Base64UrlEncoder.DecodeBytes(material.X),
                Y = Base64UrlEncoder.DecodeBytes(material.Y)
            },
            D = Base64UrlEncoder.DecodeBytes(material.D)
        });
    }

    private static string CacheKey(string sessionId) => $"singpass:v3:{sessionId}";

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static void AddIfConfigured(IDictionary<string, string> form, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            form[key] = value;
    }

    private sealed record SingpassAuthSession(
        string State,
        string Nonce,
        string CodeVerifier,
        DpopKeyMaterial DpopKey);

    private sealed record DpopKeyMaterial(string X, string Y, string D);

    private sealed record PushedAuthorizationResponse(
        [property: JsonPropertyName("request_uri")] string RequestUri,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record TokenEndpointResponse(
        [property: JsonPropertyName("id_token")] string IdToken,
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("token_type")] string? TokenType);
}
