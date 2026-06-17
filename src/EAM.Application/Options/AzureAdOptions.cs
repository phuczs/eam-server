namespace EAM.Application.Options;

/// <summary>
/// Binds the "AzureAd" configuration section (Microsoft Entra ID / Azure Active Directory).
/// Placeholder — wire up OIDC/JWT validation against these values when AAD sign-in lands.
/// </summary>
public class AzureAdOptions
{
    public const string SectionName = "AzureAd";

    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];

    /// <summary>Expected audience for AAD-issued access tokens (e.g. api://{clientId}).</summary>
    public string Audience { get; set; } = string.Empty;
}
