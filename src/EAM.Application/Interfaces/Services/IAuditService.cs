using EAM.Application.Common;
using EAM.Application.DTOs.Audits;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Services
{
    public interface IAuditService
    {
        Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(AuditLogSearchRequest request);
    }

}
