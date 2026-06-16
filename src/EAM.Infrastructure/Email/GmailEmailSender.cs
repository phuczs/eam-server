using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Options;
using System.Text;

namespace EAM.Infrastructure.Email;

/// <summary>
/// Gmail API email sender.
/// Flow: SendAsync → build Gmail service (OAuth2 refresh token) → Gmail API → sent.
/// </summary>
public sealed class GmailEmailSender : IEmailSender
{
    private readonly GmailOptions _opts;
    private readonly ILogger<GmailEmailSender> _logger;

    public GmailEmailSender(
        IOptions<GmailOptions> opts,
        ILogger<GmailEmailSender> logger)
    {
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail, string subject, string htmlBody,
        EmailCategory category = EmailCategory.Notification, CancellationToken ct = default)
    {
        var normalised = toEmail.Trim().ToLowerInvariant();

        _logger.LogInformation("[Email] Sending -> {To} | Subject: {Subject}", normalised, subject);

        try
        {
            var service = await BuildGmailServiceAsync(ct);
            var raw = BuildRawMessage(normalised, subject, htmlBody);
            await service.Users.Messages
                         .Send(new Message { Raw = raw }, "me")
                         .ExecuteAsync(ct);

            _logger.LogInformation("[Email] Sent successfully -> {To}", normalised);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send -> {To} | Subject: {Subject}", normalised, subject);
            throw;
        }
    }

    // ── Gmail service builder (OAuth2 refresh-token flow) ───────────────────
    private async Task<GmailService> BuildGmailServiceAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opts.ClientId))
            throw new InvalidOperationException("Gmail:ClientId is not configured.");
        if (string.IsNullOrWhiteSpace(_opts.ClientSecret))
            throw new InvalidOperationException("Gmail:ClientSecret is not configured.");
        if (string.IsNullOrWhiteSpace(_opts.RefreshToken))
            throw new InvalidOperationException("Gmail:RefreshToken is not configured.");
        if (string.IsNullOrWhiteSpace(_opts.SenderEmail))
            throw new InvalidOperationException("Gmail:SenderEmail is not configured.");

        var flow = new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(
            new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _opts.ClientId,
                    ClientSecret = _opts.ClientSecret,
                },
                Scopes = [GmailService.Scope.GmailSend],
                DataStore = new FileDataStore("EAM.GmailToken", fullPath: false),
            });

        var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse { RefreshToken = _opts.RefreshToken };
        var credential = new UserCredential(flow, _opts.SenderEmail, token);

        await Task.CompletedTask; // builder is sync; keep the async signature for symmetry

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "EAM Platform",
        });
    }

    // ── RFC 2822 raw message builder ─────────────────────────────────────────
    private string BuildRawMessage(string to, string subject, string htmlBody)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"From: {_opts.SenderDisplayName} <{_opts.SenderEmail}>");
        sb.AppendLine($"To: {to}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine("Content-Type: text/html; charset=utf-8");
        sb.AppendLine("Content-Transfer-Encoding: base64");
        sb.AppendLine();
        sb.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlBody)));

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()))
                      .Replace('+', '-')
                      .Replace('/', '_')
                      .TrimEnd('=');
    }
}
