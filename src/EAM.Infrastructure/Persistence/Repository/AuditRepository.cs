namespace EAM.Infrastructure.Repositories;

using EAM.Application.Interfaces.Repositories;
using EAM.Domain.Entities;
using EAM.Infrastructure.Persistence;
using Google;
using Microsoft.EntityFrameworkCore;

public class AuditRepository : IAuditRepository
{
    private readonly EamDbContext _context;

    public AuditRepository(EamDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<AuditLog> Items, int Total)> GetPagedAsync(
        Guid? actorUserId,
        Guid? entityId,
        string? action,
        string? entityType,
        int skip,
        int take)
    {
        var query = _context.Set<AuditLog>().AsNoTracking();

        if (actorUserId.HasValue)
        {
            query = query.Where(a => a.ActorUserId == actorUserId.Value);
        }
        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(action))
        {
            var termAction = action.Trim().ToLower();
            query = query.Where(a => a.Action.ToLower() == termAction);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var termEntity = entityType.Trim().ToLower();
            query = query.Where(a => a.EntityType.ToLower() == termEntity);
        }

        query = query.OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(AuditLog auditLog)
    {
        await _context.Set<AuditLog>().AddAsync(auditLog);
        await _context.SaveChangesAsync();
    }
}