namespace EAM.Application.DTOs.AuditLogs.Response;

public record AuditLogResponse(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? OldValuesJson,
    string? NewValuesJson,
    string? MetadataJson,
    string? IpAddress,
    string? UserAgent,
    string? RequestId,
    string? CorrelationId,
    DateTime CreatedAt);
