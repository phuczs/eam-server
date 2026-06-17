namespace EAM.Application.Interfaces.Repositories;

using EAM.Domain.Entities;

public interface IAuditRepository
{
    Task<(IEnumerable<AuditLog> Items, int Total)> GetPagedAsync(
        Guid? actorUserId,
        Guid? entityId,
        string? action,
        string? entityType,
        int skip,
        int take);

    Task AddAsync(AuditLog auditLog);

    
}