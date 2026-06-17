namespace EAM.Application.DTOs.Users.Request;

public class CreateUserRequest
{
    public string Role { get; set; } = "user";
    public string? OfficialId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ResidentialAddress { get; set; }
    public Guid? AccountCreatedByUserId { get; set; }
}
