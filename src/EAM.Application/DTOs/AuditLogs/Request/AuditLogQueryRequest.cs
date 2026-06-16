namespace EAM.Application.DTOs.AuditLogs.Request;

public record AuditLogQueryRequest(
    Guid? ActorUserId,
    string? Action,
    string? EntityType,
    Guid? EntityId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? CorrelationId,
    int Page = 1,
    int PageSize = 50);
