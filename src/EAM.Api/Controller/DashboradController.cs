using EAM.Application.Common;
using EAM.Application.DTOs.Dashboard;
using EAM.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAM.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
//[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    //Us-18
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary()
    {
        var data = await _dashboardService.GetDashboardDataAsync();
        return Ok(ApiResponse<DashboardSummaryResponse>.Ok(data));
    }

    /// <summary>
    //Us-18
    /// </summary>
    [HttpGet("pending-closure")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetPendingClosure([FromQuery] PageRequest request)
    {
        var data = await _dashboardService.GetPendingClosureListAsync(request);
        return Ok(ApiResponse<PagedResult<UserResponse>>.Ok(data));
    }
}