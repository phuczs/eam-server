namespace EAM.Application.Interfaces.Services;

/// <summary>High-level, template-based email API used by application services.</summary>
public interface IEmailService
{
    Task SendOtpAsync(string to, string displayName, string code, CancellationToken ct = default);

    Task SendInvitationAsync(
        string to,
        string displayName,
        string inviterName,
        string role,
        string loginUrl,
        string tempPassword,
        string[] products,
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string to,
        string displayName,
        string resetUrl,
        CancellationToken ct = default);

    Task SendWelcomeAsync(
        string to,
        string displayName,
        string signInUrl,
        CancellationToken ct = default);
}
