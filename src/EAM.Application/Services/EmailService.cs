using EAM.Application.Email;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Services;

namespace EAM.Application.Services;

/// <summary>
/// Template wrapper over <see cref="IEmailSender"/>: builds branded HTML bodies and
/// tags each message with the correct <see cref="EmailCategory"/>.
/// </summary>
public sealed class EmailService(IEmailSender sender) : IEmailService
{
    // ── Transactional — essential for account access ──

    public Task SendOtpAsync(string to, string displayName, string code, CancellationToken ct = default) =>
        sender.SendAsync(to,
            subject: "Your EAM Platform Verification Code",
            body: EmailTemplates.OtpVerification(displayName, code),
            category: EmailCategory.Transactional, ct: ct);

    public Task SendInvitationAsync(
        string to, string displayName, string inviterName,
        string role, string loginUrl, string tempPassword,
        string[] products, CancellationToken ct = default) =>
        sender.SendAsync(to,
            subject: "You've been invited to EAM Platform",
            body: EmailTemplates.UserInvitation(displayName, inviterName, role, loginUrl, tempPassword, products),
            category: EmailCategory.Transactional, ct: ct);

    public Task SendPasswordResetAsync(string to, string displayName, string resetUrl, CancellationToken ct = default) =>
        sender.SendAsync(to,
            subject: "Reset Your EAM Platform Password",
            body: EmailTemplates.PasswordReset(displayName, resetUrl),
            category: EmailCategory.Transactional, ct: ct);

    // ── Notification — non-critical ──

    public Task SendWelcomeAsync(string to, string displayName, string signInUrl, CancellationToken ct = default) =>
        sender.SendAsync(to,
            subject: "Welcome to EAM Platform",
            body: EmailTemplates.Welcome(displayName, signInUrl),
            category: EmailCategory.Notification, ct: ct);
}
