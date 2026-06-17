using EAM.Application.Common;
using EAM.Application.DTOs.Dashboard;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryResponse> GetDashboardDataAsync();
        Task<PagedResult<UserResponse>> GetPendingClosureListAsync(PageRequest request);

    }
}
