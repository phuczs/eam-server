namespace EAM.Application.Options;

/// <summary>Gmail API (OAuth2 refresh-token) settings for the email sender.</summary>
public class GmailOptions
{
    public const string SectionName = "Gmail";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = "EAM Platform";
}
