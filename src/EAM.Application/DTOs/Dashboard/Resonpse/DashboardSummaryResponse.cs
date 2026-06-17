namespace EAM.Application.DTOs.Dashboard;

using EAM.Application.DTOs.Audits;

public class DashboardSummaryResponse
{
    public AccountStats Stats { get; set; } = new();

    public List<UserException> Alerts { get; set; } = new();

    public List<UserResponse> PendingClosureList { get; set; } = new();

    public List<AuditLogResponse> RecentLogs { get; set; } = new();
}

public class AccountStats
{
    public int TotalActive { get; set; }
    public int TotalPendingClosure { get; set; }
    public int TotalClosed { get; set; }
}