using Microsoft.Extensions.Logging;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Domain.Enums;

namespace EAM.Infrastructure.Logging;

/// <summary>
/// Central audit writer. The reference project persists append-only rows to the audit
/// table; until the EAM domain + persistence layer exist, this skeleton emits a
/// structured log entry (still append-only, still survives a later failure in the
/// surrounding operation). Swap the body for a DbContext write once entities land.
/// </summary>
public class AuditWriter : IAuditWriter
{
    private readonly ICurrentUserAccessor _current;
    private readonly IRequestContext _request;
    private readonly ILogger<AuditWriter> _logger;

    public AuditWriter(ICurrentUserAccessor current, IRequestContext request, ILogger<AuditWriter> logger)
    {
        _current = current;
        _request = request;
        _logger = logger;
    }

    public Task WriteAsync(
        AuditAction action,
        string objectType,
        string? objectId = null,
        AuditOutcome outcome = AuditOutcome.Success,
        string? oldValue = null,
        string? newValue = null,
        string? actorOverride = null,
        Guid? actorUserIdOverride = null,
        string? sourceIp = null,
        CancellationToken ct = default)
    {
        var me = _current.Current;
        var actor = actorOverride ?? me?.Email ?? "System/Anonymous";
        var actorId = actorUserIdOverride ?? me?.UserId;
        var ip = sourceIp ?? _request.SourceIp;

        _logger.LogInformation(
            "AUDIT {Action} {ObjectType} {ObjectId} outcome={Outcome} actor={Actor} actorId={ActorId} ip={Ip} old={Old} new={New}",
            action, objectType, objectId ?? string.Empty, outcome, actor, actorId, ip, oldValue, newValue);

        return Task.CompletedTask;
    }
}
