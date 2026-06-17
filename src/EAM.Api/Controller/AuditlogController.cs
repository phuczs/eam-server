namespace EAM.Api.Controller
{
    using EAM.Application.Common;
    using EAM.Application.DTOs.Audits;
    using EAM.Application.Interfaces.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    //[Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<AuditLogResponse>>>> GetLogs([FromQuery] AuditLogSearchRequest request)
        {
            var result = await _auditService.GetAuditLogsAsync(request);

            return Ok(ApiResponse<PagedResult<AuditLogResponse>>.Ok(result));
        }
    }
}
