namespace EAM.Application.Interfaces.Infrastructures;

/// <summary>Classifies an outbound email.</summary>
public enum EmailCategory
{
    Notification,
    Transactional
}

/// <summary>Low-level transport that delivers a single email.</summary>
public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string body,
        EmailCategory category = EmailCategory.Notification,
        CancellationToken ct = default);
}
