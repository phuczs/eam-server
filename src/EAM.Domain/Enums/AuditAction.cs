namespace EAM.Domain.Enums;

/// <summary>Audited operations recorded by the audit trail.</summary>
public enum AuditAction
{
    SignIn = 1,
    SignOut = 2,
    Create = 3,
    Update = 4,
    Delete = 5,
    Deactivate = 6,
    LoginFailed = 8,
    MfaIssued = 9,
    MfaVerified = 10
}

/// <summary>Result of an audited operation.</summary>
public enum AuditOutcome
{
    Success = 1,
    Failure = 2
}
