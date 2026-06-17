namespace EAM.Application.DTOs.Users.Response;

/// <summary>
/// Tailored response for the e-service portal "view my profile" endpoint.
/// Contains only the fields a regular authenticated user needs to see.
/// Sensitive values (NRIC) are pre-masked by the AutoMapper profile before
/// this DTO ever leaves the application layer.
/// </summary>
public class UserProfileResponse
{
    public Guid Id { get; set; }

    /// <summary>NRIC / Official ID — full value returned (user is viewing their own record).</summary>
    public string? OfficialId { get; set; }

    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? ResidentialAddress { get; set; }

    public string AccountStatus { get; set; } = null!;
    public string IdentityLinkStatus { get; set; } = null!;

    public DateTime? AccountActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
