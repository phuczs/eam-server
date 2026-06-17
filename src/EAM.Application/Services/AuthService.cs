using EAM.Application.Common;
using EAM.Application.DTOs.Auth.Request;
using EAM.Application.DTOs.Auth.Response;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Repositories;
using EAM.Application.Interfaces.Services;
using EAM.Application.Options;
using EAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace EAM.Application.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IExternalIdentityRepository _identities;
        private readonly IRefreshTokenRepository _tokens;
        private readonly ISingpassOidcService _singpass;
        private readonly IAzureAdOidcService _azureAd;
        private readonly IJwtTokenService _jwt;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IExternalIdentityRepository identities,
            IRefreshTokenRepository tokens,

            ISingpassOidcService singpass,
            IAzureAdOidcService azureAd,
            IJwtTokenService jwt,
            ILogger<AuthService> logger)
        {
            _identities = identities;
            _tokens = tokens;

            _singpass = singpass;
            _azureAd = azureAd;
            _jwt = jwt;
            _logger = logger;
        }

        public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
            => throw new AppException("Local email/password login is not implemented. Use Singpass.");

        public async Task<AuthResult> ExternalLoginAsync(
            string provider,
            string authorizationCode,
            string state,
            string sessionId,
            CancellationToken ct = default)
        {
            var normalizedProvider = NormalizeProvider(provider);
            var subject = normalizedProvider switch
            {
                "singpass" => await _singpass.ExchangeCodeForSubjectAsync(authorizationCode, state, sessionId, ct),
                "azure_ad" => await _azureAd.ExchangeCodeForSubjectAsync(authorizationCode, state, sessionId, ct),
                _ => throw new AppException($"Unknown external provider: {provider}")
            };

            var identity = await _identities.FindByProviderSubjectAsync(normalizedProvider, subject, ct);

            if (identity is null)
            {
                _logger.LogWarning("{Provider} login: no account linked to subject {Subject}", normalizedProvider, subject);
                throw new AppException($"No account is linked to this {normalizedProvider} identity.");
            }

            if (identity.User.Status != "active")
                throw new AppException("This account is not active.");

            identity.LastUsedAt = DateTime.UtcNow;

            var (accessToken, expiresAt) = _jwt.IssueAccessToken(identity.User);
            var (rawRefresh, refreshEntity) = _jwt.IssueRefreshToken(identity.UserId);

            _tokens.Add(refreshEntity);
            await _tokens.SaveChangesAsync(ct);

            _logger.LogInformation("{Provider} login succeeded for user {UserId}", normalizedProvider, identity.UserId);
            return new AuthResult(accessToken, rawRefresh, expiresAt);
        }

        private static string NormalizeProvider(string provider)
            => provider.Trim().ToLowerInvariant() switch
            {
                "singpass" => "singpass",
                "azuread" => "azure_ad",
                "azure_ad" => "azure_ad",
                "aad" => "azure_ad",
                _ => provider
            };

        public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
        {
            var hash = _jwt.HashToken(refreshToken);

            var stored = await _tokens.FindActiveByHashAsync(hash, ct);

            if (stored is null || !stored.IsActive)
                throw new AppException("Invalid or expired refresh token.");

            var (accessToken, expiresAt) = _jwt.IssueAccessToken(stored.User);
            var (rawNew, newEntity) = _jwt.IssueRefreshToken(stored.UserId);

            stored.RevokedAtUtc = DateTime.UtcNow;
            stored.ReplacedByTokenHash = newEntity.TokenHash;
            stored.UpdatedAtUtc = DateTime.UtcNow;

            _tokens.Add(newEntity);
            await _tokens.SaveChangesAsync(ct);

            return new AuthResult(accessToken, rawNew, expiresAt);
        }

        public async Task LogoutAsync(Guid userId, CancellationToken ct = default)
        {
            var active = await _tokens.GetActiveByUserAsync(userId, ct);

            var now = DateTime.UtcNow;
            foreach (var t in active)
            {
                t.RevokedAtUtc = now;
                t.UpdatedAtUtc = now;
            }

            await _tokens.SaveChangesAsync(ct);
        }
    }

}
