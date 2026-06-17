namespace EAM.Application.Options;

/// <summary>
/// Binds the "Singpass" configuration section (Singpass / Corppass). In development
/// this points at <see cref="MockpassOptions"/> — a local emulator
/// (https://github.com/opengovsg/mockpass). Placeholder until the real RP flow is built.
/// </summary>
public class SingpassOptions
{
    public const string SectionName = "Singpass";

    public bool Enabled { get; set; }

    /// <summary>"Mockpass" for local dev, "Singpass" for staging/production.</summary>
    public string Mode { get; set; } = "Mockpass";

    public MockpassOptions Mockpass { get; set; } = new();

    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = ["openid"];
    public string Issuer { get; set; } = string.Empty;
    public string JwksEndpoint { get; set; } = "/.well-known/keys";
    public string AcrValues { get; set; } = "urn:singpass:authentication:loa:1";
    public string RedirectUriHttpsType { get; set; } = string.Empty;
    public string AuthenticationContextType { get; set; } = string.Empty;
    public string AuthenticationContextMessage { get; set; } = string.Empty;

    /// <summary>Key id of the RP signing key (private_key_jwt client auth).</summary>
    public string PrivateKeySigningKid { get; set; } = string.Empty;

    /// <summary>Key id of the RP key used to decrypt the ID token (JWE).</summary>
    public string PrivateKeyEncryptionKid { get; set; } = string.Empty;
}

/// <summary>Local Mockpass emulator settings used when <see cref="SingpassOptions.Mode"/> is "Mockpass".</summary>
public class MockpassOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5156";

    /// <summary>
    /// Issuer value MockPass puts in the id_token. Defaults to BaseUrl.
    /// Matches what MockPass returns in its /.well-known/openid-configuration.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// URL MockPass will use to fetch this RP's JWKS (our /api/auth/jwks endpoint).
    /// Set FAPI_CLIENT_JWKS_ENDPOINT on MockPass to this value for Singpass v3 FAPI.
    /// </summary>
    public string RpJwksEndpoint { get; set; } = "http://localhost:5000/api/auth/jwks";
}
