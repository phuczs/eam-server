using EAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EAM.Infrastructure.Persistence.Configurations;

internal sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Role).HasMaxLength(50).HasDefaultValue("user").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.IdentityLinkStatus).HasMaxLength(50).HasDefaultValue("unlinked").IsRequired();
        builder.Property(x => x.OfficialId).HasMaxLength(255);
        builder.Property(x => x.FullName).HasMaxLength(255);
        builder.Property(x => x.Email).HasMaxLength(255);
        builder.Property(x => x.Mobile).HasMaxLength(50);
        builder.Property(x => x.DateOfBirth).HasColumnType("date");
        builder.Property(x => x.ResidentialAddress).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AccountStatus).HasMaxLength(50).HasDefaultValue("pending_activation").IsRequired();
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.AccountClosureReason).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.Role).HasDatabaseName("IX_users_role");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_users_status");
        builder.HasIndex(x => x.AccountStatus).HasDatabaseName("IX_users_account_status");
        builder.HasIndex(x => x.OfficialId).HasDatabaseName("IX_users_official_id_hash");

        builder.HasOne(x => x.AccountCreatedByUser)
            .WithMany(x => x.CreatedAccounts)
            .HasForeignKey(x => x.AccountCreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ExternalIdentityConfig : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Provider).HasMaxLength(100).HasDefaultValue("singpass").IsRequired();
        builder.Property(x => x.ProviderSubjectId).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => new { x.Provider, x.ProviderSubjectId })
            .IsUnique()
            .HasDatabaseName("UX_external_identities_provider_subject");
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_external_identities_user_id");

        builder.HasOne(x => x.User)
            .WithMany(x => x.ExternalIdentities)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(256);
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsRevoked);
        builder.Ignore(x => x.IsActive);

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_refresh_tokens_user_id");
        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UX_refresh_tokens_token_hash");

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class AccountTransactionConfig : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.TransactionNo).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TransactionType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);
        builder.Property(x => x.ReferenceType).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_account_transactions_user_id");
        builder.HasIndex(x => x.TransactionNo).IsUnique().HasDatabaseName("IX_account_transactions_transaction_no");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_account_transactions_created_at");
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId }).HasDatabaseName("IX_account_transactions_reference");

        builder.HasOne(x => x.User)
            .WithMany(x => x.AccountTransactions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedAccountTransactions)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class TopupBatchConfig : IEntityTypeConfiguration<TopupBatch>
{
    public void Configure(EntityTypeBuilder<TopupBatch> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.BatchNo).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BatchName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(50).HasDefaultValue("manual").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("draft").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.TotalAccounts).HasDefaultValue(0);
        builder.Property(x => x.SuccessCount).HasDefaultValue(0);
        builder.Property(x => x.FailedCount).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.BatchNo).IsUnique().HasDatabaseName("IX_topup_batches_batch_no");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_topup_batches_status");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_topup_batches_created_at");

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedTopupBatches)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class TopupItemConfig : IEntityTypeConfiguration<TopupItem>
{
    public void Configure(EntityTypeBuilder<TopupItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("draft").IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.BatchId).HasDatabaseName("IX_topup_items_batch_id");
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_topup_items_user_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_topup_items_status");

        builder.HasOne(x => x.Batch)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.User)
            .WithMany(x => x.TopupItems)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Transaction)
            .WithMany(x => x.TopupItems)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class CourseConfig : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.CourseCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CourseName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("draft").IsRequired();
        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.CourseCode).IsUnique().HasDatabaseName("IX_courses_course_code");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_courses_status");

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedCourses)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class CourseEnrollmentConfig : IEntityTypeConfiguration<CourseEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.ExternalPaymentOnly).HasDefaultValue(false);
        builder.Property(x => x.EnrollmentSource).HasMaxLength(50).HasDefaultValue("manual").IsRequired();
        builder.Property(x => x.EnrolledAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnType("date");

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_course_enrollments_user_id");
        builder.HasIndex(x => x.CourseId).HasDatabaseName("IX_course_enrollments_course_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_course_enrollments_status");

        builder.HasOne(x => x.User)
            .WithMany(x => x.CourseEnrollments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Course)
            .WithMany(x => x.Enrollments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class BillingPeriodConfig : IEntityTypeConfiguration<BillingPeriod>
{
    public void Configure(EntityTypeBuilder<BillingPeriod> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.IsClosed).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => new { x.Year, x.Month }).IsUnique().HasDatabaseName("UX_billing_periods_year_month");
    }
}

internal sealed class InvoiceConfig : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.InvoiceNo).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("draft").IsRequired();
        builder.Property(x => x.SubtotalAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.GstAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.SubsidyAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.WaiverAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.OutstandingAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.GeneratedByJobId).HasMaxLength(100);
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_invoices_user_id");
        builder.HasIndex(x => x.BillingPeriodId).HasDatabaseName("IX_invoices_billing_period_id");
        builder.HasIndex(x => x.InvoiceNo).IsUnique().HasDatabaseName("IX_invoices_invoice_no");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_invoices_status");
        builder.HasIndex(x => x.DueDate).HasDatabaseName("IX_invoices_due_date");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.BillingPeriod)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.BillingPeriodId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class InvoiceItemConfig : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.FeeComponent).HasMaxLength(100);
        builder.Property(x => x.FeeType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Quantity).HasDefaultValue(1);
        builder.Property(x => x.UnitAmount).HasPrecision(18, 2);
        builder.Property(x => x.GstAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.SubsidyAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.WaiverAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.CalculationJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.InvoiceId).HasDatabaseName("IX_invoice_items_invoice_id");
        builder.HasIndex(x => x.EnrollmentId).HasDatabaseName("IX_invoice_items_enrollment_id");
        builder.HasIndex(x => x.FasAwardId).HasDatabaseName("IX_invoice_items_fas_award_id");

        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Enrollment)
            .WithMany(x => x.InvoiceItems)
            .HasForeignKey(x => x.EnrollmentId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.FasAward)
            .WithMany(x => x.InvoiceItems)
            .HasForeignKey(x => x.FasAwardId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class PaymentConfig : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.PaymentNo).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("pending").IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(255);
        builder.Property(x => x.GatewayName).HasMaxLength(100);
        builder.Property(x => x.ExternalPaymentRef).HasMaxLength(255);
        builder.Property(x => x.GatewayPayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ReceiptNo).HasMaxLength(100);
        builder.Property(x => x.ReceiptFileUrl).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.PaymentNo).IsUnique().HasDatabaseName("IX_payments_payment_no");
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_payments_user_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_payments_status");
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_payments_idempotency_key");
        builder.HasIndex(x => x.ReceiptNo)
            .IsUnique()
            .HasFilter("[ReceiptNo] IS NOT NULL")
            .HasDatabaseName("UX_payments_receipt_no");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.AccountTransaction)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.AccountTransactionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class PaymentAllocationConfig : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.PaymentId).HasDatabaseName("IX_payment_allocations_payment_id");
        builder.HasIndex(x => x.InvoiceId).HasDatabaseName("IX_payment_allocations_invoice_id");

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.PaymentAllocations)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class FasSchemeConfig : IEntityTypeConfiguration<FasScheme>
{
    public void Configure(EntityTypeBuilder<FasScheme> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("draft").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnType("date");
        builder.Property(x => x.EffectiveTo).HasColumnType("date");
        builder.Property(x => x.ApplicationStartDate).HasColumnType("date");
        builder.Property(x => x.ApplicationEndDate).HasColumnType("date");
        builder.Property(x => x.ApplicableCoursesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CriteriaJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.RequiredDocumentsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IX_fas_schemes_code");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_fas_schemes_status");

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedFasSchemes)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class FasApplicationConfig : IEntityTypeConfiguration<FasApplication>
{
    public void Configure(EntityTypeBuilder<FasApplication> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("draft").IsRequired();
        builder.Property(x => x.HouseholdIncome).HasPrecision(18, 2);
        builder.Property(x => x.PerCapitaIncome).HasPrecision(18, 2);
        builder.Property(x => x.EvaluationResultJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ReviewRemarks).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.SchemeId).HasDatabaseName("IX_fas_applications_scheme_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_fas_applications_status");

        builder.HasOne(x => x.Scheme)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.SchemeId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Enrollment)
            .WithMany(x => x.FasApplications)
            .HasForeignKey(x => x.EnrollmentId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.ReviewedByUser)
            .WithMany(x => x.ReviewedFasApplications)
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class FasApplicationEvidenceConfig : IEntityTypeConfiguration<FasApplicationEvidence>
{
    public void Configure(EntityTypeBuilder<FasApplicationEvidence> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.EvidenceType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("uploaded").IsRequired();
        builder.Property(x => x.ReviewRemarks).HasMaxLength(1000);
        builder.Property(x => x.UploadedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.ApplicationId).HasDatabaseName("IX_fas_application_evidences_application_id");
        builder.HasIndex(x => x.EvidenceType).HasDatabaseName("IX_fas_application_evidences_evidence_type");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_fas_application_evidences_status");

        builder.HasOne(x => x.Application)
            .WithMany(x => x.Evidences)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class FasAwardConfig : IEntityTypeConfiguration<FasAward>
{
    public void Configure(EntityTypeBuilder<FasAward> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnType("date");
        builder.Property(x => x.EffectiveTo).HasColumnType("date");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.ApplicationId).IsUnique().HasDatabaseName("UX_fas_awards_application_id");

        builder.HasOne(x => x.Application)
            .WithOne(x => x.Award)
            .HasForeignKey<FasAward>(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OldValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NewValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IpAddress).HasMaxLength(100);
        builder.Property(x => x.UserAgent).HasMaxLength(1000);
        builder.Property(x => x.RequestId).HasMaxLength(100);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.ActorUserId).HasDatabaseName("IX_audit_logs_actor_user_id");
        builder.HasIndex(x => x.EntityType).HasDatabaseName("IX_audit_logs_entity_type");
        builder.HasIndex(x => x.EntityId).HasDatabaseName("IX_audit_logs_entity_id");
        builder.HasIndex(x => x.Action).HasDatabaseName("IX_audit_logs_action");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_audit_logs_created_at");
        builder.HasIndex(x => x.RequestId).HasDatabaseName("IX_audit_logs_request_id");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_audit_logs_correlation_id");

        builder.HasOne(x => x.ActorUser)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
internal sealed class UserBankAccountConfig : IEntityTypeConfiguration<UserBankAccount>
{
    public void Configure(EntityTypeBuilder<UserBankAccount> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BankCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.EncryptedAccountNumber).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Last4).HasMaxLength(4).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_user_bank_accounts_user_id");
        builder.HasIndex(x => x.IsPrimary).HasDatabaseName("IX_user_bank_accounts_is_primary");

        builder.HasOne<User>()
            .WithMany(u => u.BankAccounts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}