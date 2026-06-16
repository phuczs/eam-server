using Hangfire;
using EAM.Application.Interfaces.Services;
using EAM.Application.Services;

namespace EAM.Infrastructure.Email;

/// <summary>
/// Fire-and-forget decorator for <see cref="IEmailService"/>. Every method enqueues a
/// Hangfire background job that calls the real <see cref="EmailService"/>, so the HTTP
/// request returns immediately and the send is retried by Hangfire on failure.
///
/// NOT registered by default in the skeleton (no Hangfire job store is configured).
/// To enable: configure Hangfire, then register
/// <c>services.AddScoped&lt;EmailService&gt;(); services.AddScoped&lt;IEmailService, HangfireEmailService&gt;();</c>
/// </summary>
internal sealed class HangfireEmailService : IEmailService
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireEmailService(IBackgroundJobClient jobs) => _jobs = jobs;

    public Task SendOtpAsync(string to, string displayName, string code, CancellationToken ct = default)
    {
        _jobs.Enqueue<EmailService>(x => x.SendOtpAsync(to, displayName, code, CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task SendInvitationAsync(
        string to, string displayName, string inviterName,
        string role, string loginUrl, string tempPassword,
        string[] products, CancellationToken ct = default)
    {
        _jobs.Enqueue<EmailService>(x =>
            x.SendInvitationAsync(to, displayName, inviterName, role, loginUrl, tempPassword, products, CancellationToken.None));
        return Task.CompletedTask;
    }



  

    public Task SendPasswordResetAsync(string to, string displayName, string resetUrl, CancellationToken ct = default)
    {
        _jobs.Enqueue<EmailService>(x => x.SendPasswordResetAsync(to, displayName, resetUrl, CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(string to, string displayName, string signInUrl, CancellationToken ct = default)
    {
        _jobs.Enqueue<EmailService>(x => x.SendWelcomeAsync(to, displayName, signInUrl, CancellationToken.None));
        return Task.CompletedTask;
    }
}
