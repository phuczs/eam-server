namespace EAM.Application.DTOs.Users.Request;

public record UpdateUserRequest(
    string? FullName,
    string? Email,
    string? Mobile,
    DateOnly? DateOfBirth,
    string? ResidentialAddress,
    string? Status,
    string? IdentityLinkStatus,
    string? AccountStatus,
    string? AccountClosureReason);
