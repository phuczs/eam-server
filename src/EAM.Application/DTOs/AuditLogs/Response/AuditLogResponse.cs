namespace EAM.Application.DTOs.Audits;

public class AuditLogResponse
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public Guid? EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? MetadataJson { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }
}