namespace EAM.Application.DTOs.Audits;

using EAM.Application.Common;

public class AuditLogSearchRequest : PageRequest
{
    public Guid? ActorUserId { get; set; }
    public Guid? Entityid { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
}