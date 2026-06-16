using EAM.Domain.Enums;

namespace EAM.Application.Interfaces.Infrastructures;

/// <summary>Central audit writer — records append-only audit entries.</summary>
public interface IAuditWriter
{
    Task WriteAsync(
        AuditAction action,
        string objectType,
        string? objectId = null,
        AuditOutcome outcome = AuditOutcome.Success,
        string? oldValue = null,
        string? newValue = null,
        string? actorOverride = null,
        Guid? actorUserIdOverride = null,
        string? sourceIp = null,
        CancellationToken ct = default);
}
