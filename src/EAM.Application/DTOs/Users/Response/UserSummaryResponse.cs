namespace EAM.Application.DTOs.Users.Response;

public record UserSummaryResponse(
    Guid Id,
    string Role,
    string Status,
    string IdentityLinkStatus,
    string AccountStatus,
    string? FullName,
    string? Email,
    DateTime CreatedAt);
