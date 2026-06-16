namespace EAM.Application.Email;

/// <summary>
/// Branded HTML email templates for the EAM Platform.
/// CSS lives in a plain const string (no interpolation) to avoid brace-escaping pain;
/// only the accent colour, icon, and body are interpolated by <see cref="Layout"/>.
/// </summary>
public static class EmailTemplates
{
    // ── Shared CSS — plain const, zero interpolation ─────────────────────────
    private const string Css = @"
    body,table,td,a{-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%}
    table,td{mso-table-lspace:0;mso-table-rspace:0;border-collapse:collapse}
    body{margin:0;padding:0;background:#F1F5F9;font-family:'Segoe UI',Arial,sans-serif}
    .wrapper{width:100%;max-width:600px;margin:40px auto;padding:0 16px}
    .header{border-radius:12px 12px 0 0;padding:32px 40px;text-align:center}
    .header-logo{font-size:22px;font-weight:700;color:#fff;letter-spacing:-.5px}
    .header-logo span{color:#93C5FD}
    .icon-wrap{width:64px;height:64px;border-radius:50%;margin:20px auto 0;line-height:64px;text-align:center}
    .card{background:#fff;padding:40px;border-left:1px solid #E2E8F0;border-right:1px solid #E2E8F0}
    .card h1{margin:0 0 8px;font-size:22px;font-weight:600;color:#0F172A;letter-spacing:-.3px}
    .card .subtitle{margin:0 0 28px;font-size:14px;color:#64748B;line-height:1.6}
    .card p{font-size:14px;color:#334155;line-height:1.7;margin:0 0 16px}
    .otp-box{margin:28px 0;background:#F8FAFF;border-radius:12px;padding:24px;text-align:center}
    .otp-label{font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:1.5px;color:#64748B;margin-bottom:12px}
    .otp-code{font-size:42px;font-weight:700;letter-spacing:12px;font-family:'Courier New',monospace}
    .otp-timer{margin-top:12px;font-size:12px;color:#94A3B8}
    .otp-timer strong{color:#475569}
    .btn-wrap{text-align:center;margin:28px 0}
    .btn{display:inline-block;color:#fff !important;text-decoration:none;font-size:14px;font-weight:600;padding:14px 32px;border-radius:8px}
    .info-table{width:100%;background:#F8FAFC;border:1px solid #E2E8F0;border-radius:8px;border-collapse:collapse;margin:20px 0}
    .info-table td{padding:10px 16px;font-size:13px;border-bottom:1px solid #E2E8F0}
    .info-table tr:last-child td{border-bottom:none}
    .lbl{color:#64748B;font-weight:600;width:140px;white-space:nowrap}
    .val{color:#1E293B}
    .alert{background:#FFF7ED;border-left:4px solid #F97316;border-radius:0 8px 8px 0;padding:14px 16px;margin:20px 0;font-size:13px;color:#7C2D12;line-height:1.6}
    .divider{border:none;border-top:1px solid #E2E8F0;margin:24px 0}
    .footer{background:#F8FAFC;border:1px solid #E2E8F0;border-top:none;border-radius:0 0 12px 12px;padding:24px 40px;text-align:center}
    .footer p{margin:0;font-size:12px;color:#94A3B8;line-height:1.8}
    .footer a{color:#2563EB;text-decoration:none}
    .note{font-size:12px;color:#94A3B8;line-height:1.7;margin-top:8px;font-style:italic}";

    // ── SVG icons ────────────────────────────────────────────────────────────
    private const string IconShield = "<svg width=\"28\" height=\"28\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#60A5FA\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/><polyline points=\"9 12 11 14 15 10\"/></svg>";
    private const string IconMail = "<svg width=\"28\" height=\"28\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#34D399\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z\"/><polyline points=\"22,6 12,13 2,6\"/></svg>";
    private const string IconUser = "<svg width=\"28\" height=\"28\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#F472B6\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2\"/><circle cx=\"12\" cy=\"7\" r=\"4\"/></svg>";

    // ── Layout builder ───────────────────────────────────────────────────────
    private static string Layout(string accentHex, string icon, string body)
    {
        string headerBg = $"background:linear-gradient(135deg,#1E40AF,{accentHex})";
        string iconStyle = $"border:2px solid {accentHex}55;background:{accentHex}22";
        string year = DateTime.UtcNow.Year.ToString();

        return
            "<!DOCTYPE html><html lang=\"en\"><head>" +
            "<meta charset=\"UTF-8\"/>" +
            "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>" +
            "<title>EAM Platform</title>" +
            "<style>" + Css + "</style>" +
            "</head><body>" +
            "<div class=\"wrapper\">" +
            "<div class=\"header\" style=\"" + headerBg + "\">" +
            "<div class=\"header-logo\">EAM<span> Platform</span></div>" +
            "<div class=\"icon-wrap\" style=\"" + iconStyle + "\">" + icon + "</div>" +
            "</div>" +
            "<div class=\"card\">" + body + "</div>" +
            "<div class=\"footer\">" +
            "<p>This email was sent by <strong>EAM Platform</strong>.<br/>" +
            "If you did not request this, please ignore it or <a href=\"#\">contact support</a>.</p>" +
            "<p style=\"margin-top:8px\">&copy; " + year + " EAM Platform. All rights reserved.</p>" +
            "</div></div></body></html>";
    }

    // ── 1. OTP / MFA verification ──────────────────────────────────────────────
    public static string OtpVerification(string displayName, string code, int ttlMinutes = 3)
    {
        string body =
            "<h1>Verification Code</h1>" +
            "<p class=\"subtitle\">Complete your sign-in to EAM Platform</p>" +
            "<p>Hi <strong>" + displayName + "</strong>,</p>" +
            "<p>Use the one-time code below to verify your identity. " +
            "Do not share this code with anyone.</p>" +
            "<div class=\"otp-box\" style=\"border:2px dashed #2563EB\">" +
            "<div class=\"otp-label\">Your verification code</div>" +
            "<div class=\"otp-code\" style=\"color:#2563EB\">" + code + "</div>" +
            "<div class=\"otp-timer\">Expires in <strong>" + ttlMinutes + " minutes</strong>" +
            " &nbsp;&middot;&nbsp; Single use only</div>" +
            "</div>" +
            "<p class=\"note\">This code cannot be reused after it has been verified or expired.</p>";
        return Layout("#2563EB", IconShield, body);
    }

    // ── 2. User invitation ─────────────────────────────────────────────────────
    public static string UserInvitation(
        string displayName, string inviterName, string role,
        string loginUrl, string tempPassword, string[] products)
    {
        string productList = products.Length > 0 ? string.Join(", ", products) : "&mdash;";
        string body =
            "<h1>You've Been Invited</h1>" +
            "<p class=\"subtitle\">Welcome to EAM Platform</p>" +
            "<p>Hi <strong>" + displayName + "</strong>,</p>" +
            "<p><strong>" + inviterName + "</strong> has invited you to join " +
            "<strong>EAM Platform</strong> as a <strong>" + role + "</strong>.</p>" +
            "<table class=\"info-table\"><tbody>" +
            "<tr><td class=\"lbl\">Role</td><td class=\"val\">" + role + "</td></tr>" +
            "<tr><td class=\"lbl\">Products</td><td class=\"val\">" + productList + "</td></tr>" +
            "<tr><td class=\"lbl\">Temp password</td>" +
            "<td class=\"val\" style=\"font-family:'Courier New',monospace;color:#059669;font-weight:700\">" + tempPassword + "</td></tr>" +
            "</tbody></table>" +
            "<div class=\"btn-wrap\">" +
            "<a href=\"" + loginUrl + "\" class=\"btn\" style=\"background:#059669\">Activate My Account &rarr;</a></div>" +
            "<div class=\"alert\">&#9888;&nbsp; This is a temporary password. You will be required to change it on first sign-in.</div>";
        return Layout("#059669", IconUser, body);
    }

    // ── 3. Password reset ──────────────────────────────────────────────────────
    public static string PasswordReset(string displayName, string resetUrl, int expiryMinutes = 60)
    {
        string body =
            "<h1>Reset Your Password</h1>" +
            "<p class=\"subtitle\">EAM Platform account recovery</p>" +
            "<p>Hi <strong>" + displayName + "</strong>,</p>" +
            "<p>We received a request to reset your password. Click the button below to choose a new one.</p>" +
            "<div class=\"btn-wrap\">" +
            "<a href=\"" + resetUrl + "\" class=\"btn\" style=\"background:#2563EB\">Reset Password &rarr;</a></div>" +
            "<div class=\"alert\">&#8987;&nbsp; This link expires in <strong>" + expiryMinutes + " minutes</strong>. " +
            "If you did not request this, you can safely ignore this email.</div>";
        return Layout("#2563EB", IconMail, body);
    }

    // ── 6. Welcome ─────────────────────────────────────────────────────────────
    public static string Welcome(string displayName, string signInUrl)
    {
        string body =
            "<h1>Welcome to EAM Platform</h1>" +
            "<p class=\"subtitle\">Your account is ready</p>" +
            "<p>Hi <strong>" + displayName + "</strong>,</p>" +
            "<p>Your EAM Platform account has been created. Sign in to get started.</p>" +
            "<div class=\"btn-wrap\">" +
            "<a href=\"" + signInUrl + "\" class=\"btn\" style=\"background:#2563EB\">Sign In &rarr;</a></div>";
        return Layout("#2563EB", IconUser, body);
    }
}
