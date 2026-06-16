namespace EAM.Application.DTOs.Users.Response;

public record UserResponse(
    Guid Id,
    string Role,
    string Status,
    string IdentityLinkStatus,
    string? OfficialId,
    string? FullName,
    string? Email,
    string? Mobile,
    DateOnly? DateOfBirth,
    string? ResidentialAddress,
    string AccountStatus,
    decimal CurrentBalance,
    DateTime? AccountActivatedAt,
    DateTime? AccountPendingClosureAt,
    DateTime? AccountClosedAt,
    string? AccountClosureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
