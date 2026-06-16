using EAM.Application.DTOs.Auth.Request;
using EAM.Application.DTOs.Auth.Response;

namespace EAM.Application.Interfaces.Services;

/// <summary>
/// Authentication flows: local credentials plus external providers
/// (Microsoft Entra ID / AAD and Singpass — Mockpass in development).
/// </summary>
public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default);

    Task LogoutAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Completes an external sign-in. <paramref name="provider"/> is e.g. "AzureAd" or "Singpass";
    /// <paramref name="authorizationCode"/> is the code returned by the provider's redirect.
    /// </summary>
    Task<AuthResult> ExternalLoginAsync(string provider, string authorizationCode, CancellationToken ct = default);
}
