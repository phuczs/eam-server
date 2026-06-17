namespace EAM.Application.Services;

using AutoMapper;
using EAM.Application.Common;
using EAM.Application.DTOs.Audits;
using EAM.Application.Interfaces.Repositories;
using EAM.Application.Interfaces.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IMapper _mapper;

    public AuditService(IAuditRepository auditRepository, IMapper mapper)
    {
        _auditRepository = auditRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(AuditLogSearchRequest request)
    {
        var (items, total) = await _auditRepository.GetPagedAsync(
            request.ActorUserId,
            request.Entityid,
            request.Action,
            request.EntityType,
            request.Skip,
            request.Size
        );

        return new PagedResult<AuditLogResponse>
        {
            Items = _mapper.Map<IReadOnlyList<AuditLogResponse>>(items),
            Total = total,
            Page = request.Page,
            Size = request.Size
        };
    }
}