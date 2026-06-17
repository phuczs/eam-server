using AutoMapper;
using EAM.Application.Common;
using EAM.Application.DTOs.Audits;
using EAM.Application.DTOs.Dashboard;
using EAM.Application.Interfaces.Repositories;
using EAM.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly IMapper _mapper;

        public DashboardService(
            IUserRepository userRepository,
            IAuditRepository auditRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _auditRepository = auditRepository;
            _mapper = mapper;
        }

        public async Task<DashboardSummaryResponse> GetDashboardDataAsync()
        {
            var stats = new AccountStats
            {
                TotalActive = await _userRepository.GetCountByStatusAsync("active"),
                TotalPendingClosure = await _userRepository.GetCountByStatusAsync("pending_closure"),
                TotalClosed = await _userRepository.GetCountByStatusAsync("closed")
            };

            var alerts = await _userRepository.GetExceptionAlertsAsync();
            //take 10
            var (logs, _) = await _auditRepository.GetPagedAsync(null, null, null, null, 0, 10);

            return new DashboardSummaryResponse
            {
                Stats = stats,
                Alerts = alerts,
                RecentLogs = _mapper.Map<List<AuditLogResponse>>(logs)
            };
        }

        public async Task<PagedResult<UserResponse>> GetPendingClosureListAsync(PageRequest request)
        {
            var (users, total) = await _userRepository.GetPagedPendingClosureAsync(request.Skip, request.Size);

            return new PagedResult<UserResponse>
            {
                Items = _mapper.Map<List<UserResponse>>(users),
                Total = total,
                Page = request.Page,
                Size = request.Size
            };
        }
    }
}
