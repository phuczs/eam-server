using EAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAM.Infrastructure.Persistence;

public class EamDbContext : DbContext
{
    public EamDbContext(DbContextOptions<EamDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
    public DbSet<TopupBatch> TopupBatches => Set<TopupBatch>();
    public DbSet<TopupItem> TopupItems => Set<TopupItem>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<BillingPeriod> BillingPeriods => Set<BillingPeriod>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<FasScheme> FasSchemes => Set<FasScheme>();
    public DbSet<FasApplication> FasApplications => Set<FasApplication>();
    public DbSet<FasApplicationEvidence> FasApplicationEvidences => Set<FasApplicationEvidence>();
    public DbSet<FasAward> FasAwards => Set<FasAward>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EamDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
