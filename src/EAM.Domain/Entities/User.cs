namespace EAM.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Role { get; set; } = "user";
    public string Status { get; set; } = "active";
    public string IdentityLinkStatus { get; set; } = "unlinked";

    public string? OfficialId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ResidentialAddress { get; set; }

    public string AccountStatus { get; set; } = "pending_activation";
    public Guid? AccountCreatedByUserId { get; set; }
    public User? AccountCreatedByUser { get; set; }

    public decimal CurrentBalance { get; set; } = 0;
    public DateTime? AccountActivatedAt { get; set; }
    public DateTime? AccountPendingClosureAt { get; set; }
    public DateTime? AccountClosedAt { get; set; }
    public string? AccountClosureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<User> CreatedAccounts { get; set; } = [];
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<AccountTransaction> AccountTransactions { get; set; } = [];
    public ICollection<AccountTransaction> CreatedAccountTransactions { get; set; } = [];
    public ICollection<TopupBatch> CreatedTopupBatches { get; set; } = [];
    public ICollection<TopupItem> TopupItems { get; set; } = [];
    public ICollection<Course> CreatedCourses { get; set; } = [];
    public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<FasScheme> CreatedFasSchemes { get; set; } = [];
    public ICollection<FasApplication> ReviewedFasApplications { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
