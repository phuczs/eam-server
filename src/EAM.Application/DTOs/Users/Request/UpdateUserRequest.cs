namespace EAM.Application.DTOs.Users.Request;

public class UpdateUserRequestDto
{
    public string? FullName { get; set; }
    public string? Mobile { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ResidentialAddress { get; set; }

    public string? Role { get; set; }
    public string? Status { get; set; }
    public string? IdentityLinkStatus { get; set; }
    public string? AccountStatus { get; set; }
}
