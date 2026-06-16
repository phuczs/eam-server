namespace EAM.Application.Common;

/// <summary>Ambient context resolved from the access token for the current request.</summary>
public class CurrentUser
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public bool IsAdmin => Roles.Contains("ServiceAdmin")
                        || Roles.Contains("TenantUserAdmin")
                        || Roles.Contains("ServiceSupportAdmin");
}
