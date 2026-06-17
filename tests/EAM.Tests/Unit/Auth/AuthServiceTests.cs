using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using EAM.Application.Common;
using EAM.Application.DTOs.Auth.Request;
using EAM.Application.Interfaces;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Repositories;
using EAM.Application.Interfaces.Services;
using EAM.Application.Services;
using EAM.Domain.Entities;

namespace EAM.Tests.Unit.Auth;

public class AuthServiceTests
{
    private readonly Mock<IExternalIdentityRepository> _identities = new();
    private readonly Mock<IRefreshTokenRepository> _tokens = new();

    private readonly Mock<ISingpassOidcService> _singpass = new();
    private readonly Mock<IAzureAdOidcService> _azureAd = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    private AuthService BuildSut() => new(
        _identities.Object, _tokens.Object,
        _singpass.Object, _azureAd.Object, _jwt.Object,
        NullLogger<AuthService>.Instance);

    // ── ExternalLoginAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ExternalLoginAsync_returns_tokens_for_active_linked_user()
    {
        var user = new User { Role = "user", Status = "active" };
        var identity = new ExternalIdentity { User = user, UserId = user.Id, Provider = "singpass", ProviderSubjectId = "S1234567A" };

        _singpass.Setup(s => s.ExchangeCodeForSubjectAsync("code-abc", "state-abc", "session-abc", default)).ReturnsAsync("S1234567A");
        _identities.Setup(r => r.FindByProviderSubjectAsync("singpass", "S1234567A", default)).ReturnsAsync(identity);
        _jwt.Setup(j => j.IssueAccessToken(user)).Returns(("access-tok", DateTime.UtcNow.AddMinutes(15)));
        _jwt.Setup(j => j.IssueRefreshToken(user.Id))
            .Returns(("refresh-raw", new RefreshToken { UserId = user.Id, TokenHash = "h", ExpiresAtUtc = DateTime.UtcNow.AddDays(7) }));

        var result = await BuildSut().ExternalLoginAsync("Singpass", "code-abc", "state-abc", "session-abc");

        result.AccessToken.Should().Be("access-tok");
        result.RefreshToken.Should().Be("refresh-raw");
        _tokens.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Once);

    }

    [Fact]
    public async Task ExternalLoginAsync_returns_tokens_for_active_linked_azure_ad_user()
    {
        var user = new User { Role = "admin", Status = "active" };
        var identity = new ExternalIdentity { User = user, UserId = user.Id, Provider = "azure_ad", ProviderSubjectId = "aad-oid" };

        _azureAd.Setup(s => s.ExchangeCodeForSubjectAsync("aad-code", "aad-state", "aad-session", default)).ReturnsAsync("aad-oid");
        _identities.Setup(r => r.FindByProviderSubjectAsync("azure_ad", "aad-oid", default)).ReturnsAsync(identity);
        _jwt.Setup(j => j.IssueAccessToken(user)).Returns(("aad-access", DateTime.UtcNow.AddMinutes(15)));
        _jwt.Setup(j => j.IssueRefreshToken(user.Id))
            .Returns(("aad-refresh", new RefreshToken { UserId = user.Id, TokenHash = "aad-h", ExpiresAtUtc = DateTime.UtcNow.AddDays(7) }));

        var result = await BuildSut().ExternalLoginAsync("AzureAd", "aad-code", "aad-state", "aad-session");

        result.AccessToken.Should().Be("aad-access");
        result.RefreshToken.Should().Be("aad-refresh");
        _tokens.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task ExternalLoginAsync_throws_when_no_linked_identity()
    {
        _singpass.Setup(s => s.ExchangeCodeForSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync("S9999999Z");
        _identities.Setup(r => r.FindByProviderSubjectAsync(It.IsAny<string>(), "S9999999Z", default))
                   .ReturnsAsync((ExternalIdentity?)null);

        var act = () => BuildSut().ExternalLoginAsync("Singpass", "code", "state", "session");

        await act.Should().ThrowAsync<AppException>().WithMessage("*linked*");

    }

    [Fact]
    public async Task ExternalLoginAsync_throws_for_unknown_provider()
    {
        var act = () => BuildSut().ExternalLoginAsync("GitHub", "code", "state", "session");

        await act.Should().ThrowAsync<AppException>().WithMessage("*Unknown external provider*");
    }

    [Fact]
    public async Task ExternalLoginAsync_throws_when_user_is_not_active()
    {
        var user = new User { Role = "user", Status = "suspended" };
        var identity = new ExternalIdentity { User = user, UserId = user.Id, Provider = "singpass", ProviderSubjectId = "S0000001A" };

        _singpass.Setup(s => s.ExchangeCodeForSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync("S0000001A");
        _identities.Setup(r => r.FindByProviderSubjectAsync("singpass", "S0000001A", default)).ReturnsAsync(identity);

        var act = () => BuildSut().ExternalLoginAsync("Singpass", "code", "state", "session");

        await act.Should().ThrowAsync<AppException>().WithMessage("*not active*");
    }

    // ── RefreshAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_rotates_token_and_revokes_old()
    {
        var user = new User { Role = "user", Status = "active" };
        var oldEntity = new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = "old-hash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
        };
        var newEntity = new RefreshToken { UserId = user.Id, TokenHash = "new-hash", ExpiresAtUtc = DateTime.UtcNow.AddDays(7) };

        _jwt.Setup(j => j.HashToken("raw-token")).Returns("old-hash");
        _tokens.Setup(r => r.FindActiveByHashAsync("old-hash", default)).ReturnsAsync(oldEntity);
        _jwt.Setup(j => j.IssueAccessToken(user)).Returns(("new-access", DateTime.UtcNow.AddMinutes(15)));
        _jwt.Setup(j => j.IssueRefreshToken(user.Id)).Returns(("new-raw", newEntity));

        var result = await BuildSut().RefreshAsync("raw-token");

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-raw");
        oldEntity.RevokedAtUtc.Should().NotBeNull("old token must be revoked");
        oldEntity.ReplacedByTokenHash.Should().Be("new-hash");
        _tokens.Verify(r => r.Add(newEntity), Times.Once);

    }

    [Fact]
    public async Task RefreshAsync_throws_for_unknown_token()
    {
        _jwt.Setup(j => j.HashToken(It.IsAny<string>())).Returns("no-match");
        _tokens.Setup(r => r.FindActiveByHashAsync("no-match", default)).ReturnsAsync((RefreshToken?)null);

        var act = () => BuildSut().RefreshAsync("garbage");

        await act.Should().ThrowAsync<AppException>().WithMessage("*Invalid or expired*");
    }

    // ── LogoutAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_revokes_all_active_tokens_for_user()
    {
        var userId = Guid.NewGuid();
        var t1 = new RefreshToken { UserId = userId, TokenHash = "h1", ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        var t2 = new RefreshToken { UserId = userId, TokenHash = "h2", ExpiresAtUtc = DateTime.UtcNow.AddDays(2) };

        _tokens.Setup(r => r.GetActiveByUserAsync(userId, default)).ReturnsAsync([t1, t2]);

        await BuildSut().LogoutAsync(userId);

        t1.RevokedAtUtc.Should().NotBeNull();
        t2.RevokedAtUtc.Should().NotBeNull();

    }
}
