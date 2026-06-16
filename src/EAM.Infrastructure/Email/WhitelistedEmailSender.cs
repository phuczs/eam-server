using Microsoft.Extensions.Logging;
using EAM.Application.Interfaces.Infrastructures;

namespace EAM.Infrastructure.Email;

/// <summary>
/// Log-only email sender for local/dev environments. Production swaps in
/// <see cref="GmailEmailSender"/> (or SMTP/SendGrid).
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string toEmail, string subject, string body,
        EmailCategory category = EmailCategory.Notification, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL -> {To} | {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
