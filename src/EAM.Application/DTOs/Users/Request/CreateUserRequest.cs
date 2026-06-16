namespace EAM.Application.DTOs.Users.Request;

public record CreateUserRequest(
    string Role,
    string? FullName,
    string? Email,
    string? Mobile,
    DateOnly? DateOfBirth,
    string? ResidentialAddress,
    string? OfficialId,
    string AccountStatus = "pending_activation",
    decimal CurrentBalance = 0);
