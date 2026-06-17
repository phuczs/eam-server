using AutoMapper;
using EAM.Application.Common;
using EAM.Application.DTOs.Users.Response;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Repositories;
using EAM.Application.Interfaces.Services;
using EAM.Application.Mappings;
using EAM.Application.Services;
using EAM.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace EAM.Tests.Unit.Users;

/// <summary>
/// Unit tests for <see cref="UserService.GetMyProfileAsync"/>.
///
/// Scope: Application layer only. No HTTP stack, no database.
/// The AutoMapper profile is wired with the real <see cref="MappingProfile"/>
/// so that field-mapping and NRIC handling are verified end-to-end within
/// the application boundary.
/// </summary>
public class UserServiceGetMyProfileTests
{
    // ── Dependencies ──────────────────────────────────────────────────────────

    private readonly Mock<IUserRepository>    _userRepo    = new();
    private readonly Mock<ICurrentUserAccessor> _currentUser = new();
    private readonly Mock<IAuditRepository>   _auditRepo   = new();
    private readonly IMapper                  _mapper      = BuildMapper();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IMapper BuildMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private UserService BuildSut() => new(
        _userRepo.Object,
        _mapper,
        _currentUser.Object,
        _auditRepo.Object);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfileAsync_returns_profile_for_existing_user()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id                  = userId,
            OfficialId          = "S1234567A",
            FullName            = "Jane Doe",
            Email               = "jane@example.com",
            Mobile              = "+6591234567",
            DateOfBirth         = new DateOnly(1990, 5, 15),
            ResidentialAddress  = "1 Marina Boulevard, Singapore 018989",
            AccountStatus       = "active",
            IdentityLinkStatus  = "linked",
            AccountActivatedAt  = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt           = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await BuildSut().GetMyProfileAsync(userId);

        // Assert — all profile fields are mapped correctly
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.FullName.Should().Be("Jane Doe");
        result.Email.Should().Be("jane@example.com");
        result.Mobile.Should().Be("+6591234567");
        result.DateOfBirth.Should().Be(new DateOnly(1990, 5, 15));
        result.ResidentialAddress.Should().Be("1 Marina Boulevard, Singapore 018989");
        result.AccountStatus.Should().Be("active");
        result.IdentityLinkStatus.Should().Be("linked");
        result.AccountActivatedAt.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetMyProfileAsync_returns_full_unmasked_officialId()
    {
        // Arrange — OfficialId must NOT be masked on the self-profile endpoint;
        // the user is viewing their own NRIC which they already know.
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, OfficialId = "S1234567A", AccountStatus = "active", IdentityLinkStatus = "linked" };

        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await BuildSut().GetMyProfileAsync(userId);

        // Assert — full NRIC is returned, not masked
        result.OfficialId.Should().Be("S1234567A");
        result.OfficialId.Should().NotContain("*", because: "masking must not be applied on the self-profile endpoint");
    }

    [Fact]
    public async Task GetMyProfileAsync_maps_nullable_fields_as_null_when_not_set()
    {
        // Arrange — minimal user with only required fields set
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id                = userId,
            AccountStatus     = "pending_activation",
            IdentityLinkStatus = "unlinked"
        };

        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await BuildSut().GetMyProfileAsync(userId);

        // Assert — nullable fields remain null and are not invented
        result.OfficialId.Should().BeNull();
        result.FullName.Should().BeNull();
        result.Email.Should().BeNull();
        result.Mobile.Should().BeNull();
        result.DateOfBirth.Should().BeNull();
        result.ResidentialAddress.Should().BeNull();
        result.AccountActivatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetMyProfileAsync_does_not_expose_balance_or_financial_data()
    {
        // Arrange — user has a non-zero balance; it must NOT appear in the profile DTO
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, CurrentBalance = 9_999.99m, AccountStatus = "active", IdentityLinkStatus = "linked" };

        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await BuildSut().GetMyProfileAsync(userId);

        // Assert — UserProfileResponse has no financial fields at all
        var responseType = typeof(UserProfileResponse);
        responseType.GetProperty("CurrentBalance").Should().BeNull(
            because: "financial data must not be exposed on the self-profile DTO");
        responseType.GetProperty("BankAccounts").Should().BeNull(
            because: "bank account data must not be exposed on the self-profile DTO");
    }

    // ── Not-found path ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfileAsync_throws_NotFound_when_user_does_not_exist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var act = () => BuildSut().GetMyProfileAsync(userId);

        // Assert — AppException with 404 status (not 403 — avoids user-existence enumeration)
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        ex.Which.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetMyProfileAsync_throws_NotFound_not_Forbidden_when_user_missing()
    {
        // Security: returning 403 would leak that a user with that ID exists.
        // We must always return 404 regardless of why the record is absent.
        var userId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var act = () => BuildSut().GetMyProfileAsync(userId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().NotBe(
            System.Net.HttpStatusCode.Forbidden,
            because: "a 403 would reveal that a user record with this ID exists");
    }

    // ── Repository interaction ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfileAsync_calls_repository_with_exact_userId_from_jwt()
    {
        // Verifies the service passes the caller-supplied ID straight to the repo,
        // never substituting a hardcoded or default value.
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, AccountStatus = "active", IdentityLinkStatus = "linked" };

        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await BuildSut().GetMyProfileAsync(userId);

        // Strict verification: called exactly once with the correct ID
        _userRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _userRepo.Verify(r => r.GetByIdAsync(It.Is<Guid>(id => id != userId)), Times.Never,
            "must never query for a different user's data");
    }
}
