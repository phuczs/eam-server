namespace EAM.Application.DTOs.ExternalIdentities.Response;

public record ExternalIdentityResponse(
    Guid Id,
    Guid UserId,
    string Provider,
    string ProviderSubjectId,
    DateTime CreatedAt,
    DateTime? LastUsedAt);
