using EAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAM.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(EamDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureDeletedAsync(ct);
        await db.Database.EnsureCreatedAsync(ct);

        var now = DateTime.UtcNow;

        var admin = new User
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Role = "admin",
            Status = "active",
            IdentityLinkStatus = "linked",
            OfficialId = "seed-admin-official-id-hash",
            FullName = "Seed Admin",
            Email = "admin@eam.local",
            Mobile = "+6580000001",
            DateOfBirth = new DateOnly(1990, 1, 1),
            AccountStatus = "active",
            CurrentBalance = 0,
            AccountActivatedAt = now,
            CreatedAt = now
        };

        var learner = new User
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Role = "user",
            Status = "active",
            IdentityLinkStatus = "linked",
            OfficialId = "seed-learner-official-id-hash",
            FullName = "Seed Learner",
            Email = "learner@eam.local",
            Mobile = "+6580000002",
            DateOfBirth = new DateOnly(2002, 5, 12),
            ResidentialAddress = "1 Education Way #08-01, Singapore 018989",
            AccountStatus = "active",
            AccountCreatedByUserId = admin.Id,
            CurrentBalance = 1250.00m,
            AccountActivatedAt = now,
            CreatedAt = now
        };

        db.Users.AddRange(admin, learner);
        db.ExternalIdentities.AddRange(
            new ExternalIdentity
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                UserId = admin.Id,
                Provider = "azure_ad",
                ProviderSubjectId = "seed-admin-azure-ad-subject",
                CreatedAt = now
            },
            new ExternalIdentity
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                UserId = learner.Id,
                Provider = "singpass",
                ProviderSubjectId = "seed-learner-singpass-subject",
                CreatedAt = now
            });

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
            UserId = learner.Id,
            TokenHash = "seed-refresh-token-hash",
            ExpiresAtUtc = now.AddDays(30),
            CreatedAtUtc = now
        });

        db.AccountTransactions.Add(new AccountTransaction
        {
            Id = Guid.Parse("50000000-0000-0000-0000-000000000001"),
            TransactionNo = "TXN-SEED-000001",
            UserId = learner.Id,
            TransactionType = "top_up",
            Amount = 1250.00m,
            BalanceAfter = 1250.00m,
            ReferenceType = "seed",
            Description = "Seed opening balance",
            CreatedByUserId = admin.Id,
            CreatedAt = now
        });

        db.Courses.Add(new Course
        {
            Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
            CourseCode = "SEED-COURSE",
            CourseName = "Seed Course",
            Status = "active",
            CreatedByUserId = admin.Id,
            CreatedAt = now
        });

        db.BillingPeriods.Add(new BillingPeriod
        {
            Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
            Year = now.Year,
            Month = now.Month,
            StartDate = new DateOnly(now.Year, now.Month, 1),
            EndDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)),
            CreatedAt = now
        });

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
            ActorUserId = admin.Id,
            Action = "CREATE",
            EntityType = nameof(User),
            EntityId = learner.Id,
            MetadataJson = """{"source":"seed"}""",
            CreatedAt = now
        });

        await db.SaveChangesAsync(ct);
    }
}
