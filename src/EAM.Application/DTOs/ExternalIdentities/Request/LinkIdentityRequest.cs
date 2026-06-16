namespace EAM.Application.DTOs.ExternalIdentities.Request;

public record LinkIdentityRequest(
    Guid UserId,
    string Provider,
    string ProviderSubjectId);
