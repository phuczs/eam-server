using EAM.Api.Controllers;
using EAM.Application.Common;
using EAM.Application.DTOs.Users.Response;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EAM.Tests.Unit.Users;

/// <summary>
/// Unit tests for <see cref="UserController.GetMyProfile"/>.
///
/// Scope: Controller layer only — verifies routing decisions, authorization
/// behaviour, and response envelope shape. The service is mocked, so these
/// tests run without any database, HTTP client, or AutoMapper wiring.
/// </summary>
public class UserControllerGetMyProfileTests
{
    // ── Dependencies ──────────────────────────────────────────────────────────

    private readonly Mock<IUserService>          _userService  = new();
    private readonly Mock<IAuditService>         _auditService = new();
    private readonly Mock<IPaymentService>       _paymentService = new();
    private readonly Mock<ICurrentUserAccessor>  _currentUser  = new();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private UserController BuildSut() => new(
        _userService.Object,
        _auditService.Object,
        _paymentService.Object,
        _currentUser.Object);

    private static UserProfileResponse SampleProfile(Guid userId) => new()
    {
        Id                 = userId,
        OfficialId         = "S1234567A",
        FullName           = "Jane Doe",
        Email              = "jane@example.com",
        Mobile             = "+6591234567",
        CurrentBalance     = 100.50m,
        AccountStatus      = "active",
        IdentityLinkStatus = "linked",
        CreatedAt          = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfile_returns_200_with_profile_for_authenticated_user()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var profile = SampleProfile(userId);

        _currentUser.Setup(c => c.Require()).Returns(new CurrentUser { UserId = userId });
        _userService.Setup(s => s.GetMyProfileAsync(userId)).ReturnsAsync(profile);

        // Act
        var result = await BuildSut().GetMyProfile();

        // Assert — HTTP 200 with success envelope
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);

        var body = ok.Value.Should().BeOfType<ApiResponse<UserProfileResponse>>().Subject;
        body.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(userId);
        body.Data.OfficialId.Should().Be("S1234567A");
        body.Data.FullName.Should().Be("Jane Doe");
        body.Data.CurrentBalance.Should().Be(100.50m);
    }

    [Fact]
    public async Task GetMyProfile_delegates_to_service_with_userId_from_jwt_claim()
    {
        // Arrange — verifies the controller passes the JWT-sourced ID and never
        // an arbitrary or default GUID to the service.
        var userId  = Guid.NewGuid();
        var profile = SampleProfile(userId);

        _currentUser.Setup(c => c.Require()).Returns(new CurrentUser { UserId = userId });
        _userService.Setup(s => s.GetMyProfileAsync(userId)).ReturnsAsync(profile);

        // Act
        await BuildSut().GetMyProfile();

        // Assert — service called exactly once with the correct ID
        _userService.Verify(s => s.GetMyProfileAsync(userId), Times.Once);
        _userService.Verify(
            s => s.GetMyProfileAsync(It.Is<Guid>(id => id != userId)),
            Times.Never,
            "controller must never query for a different user's data");
    }

    // ── Authorization / security ──────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfile_throws_AppException_Unauthorized_when_no_jwt_principal()
    {
        // Arrange — Require() throws AppException.Unauthorized when the
        // HttpContext has no authenticated principal (defence-in-depth guard
        // that complements the [Authorize] attribute).
        _currentUser.Setup(c => c.Require())
                    .Throws(AppException.Unauthorized("Authentication required."));

        // Act
        var act = () => BuildSut().GetMyProfile();

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        // Service must never be called if auth check fails
        _userService.Verify(s => s.GetMyProfileAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void GetMyProfile_endpoint_is_decorated_with_Authorize_attribute()
    {
        // Verifies the [Authorize] attribute is present at the action level so
        // the ASP.NET Core middleware rejects anonymous requests before the
        // action body is even reached.
        var method = typeof(UserController).GetMethod(nameof(UserController.GetMyProfile));

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: false)
               .Should().HaveCount(1, because: "the endpoint must require a valid JWT");
    }

    [Fact]
    public void GetMyProfile_endpoint_route_contains_no_user_id_path_parameter()
    {
        // Security: IDOR is structurally prevented by having no ID in the URL.
        // This test locks that design decision so refactors cannot accidentally
        // introduce a path parameter.
        var method     = typeof(UserController).GetMethod(nameof(UserController.GetMyProfile));
        var httpGetAttr = method!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), inherit: false)
            .Cast<Microsoft.AspNetCore.Mvc.HttpGetAttribute>()
            .FirstOrDefault();

        httpGetAttr.Should().NotBeNull();
        httpGetAttr!.Template.Should().Be("me/profile");
        httpGetAttr.Template.Should().NotContain("{",
            because: "no path parameter must exist — IDOR prevention by design");
    }

    // ── Not-found propagation ─────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfile_propagates_NotFound_exception_from_service()
    {
        // Arrange — service throws when the authenticated user's record is missing
        // (edge case: account deleted after token issuance).
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.Require()).Returns(new CurrentUser { UserId = userId });
        _userService.Setup(s => s.GetMyProfileAsync(userId))
                    .ThrowsAsync(AppException.NotFound("User profile", userId));

        // Act
        var act = () => BuildSut().GetMyProfile();

        // Assert — exception bubbles up unchanged for the global exception handler
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── Response envelope ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfile_wraps_result_in_ApiResponse_success_envelope()
    {
        // The API contract guarantees every 200 response is wrapped in
        // ApiResponse<T> { Success = true, Data = <payload> }.
        var userId  = Guid.NewGuid();
        var profile = SampleProfile(userId);

        _currentUser.Setup(c => c.Require()).Returns(new CurrentUser { UserId = userId });
        _userService.Setup(s => s.GetMyProfileAsync(userId)).ReturnsAsync(profile);

        var result = await BuildSut().GetMyProfile();

        var ok   = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<ApiResponse<UserProfileResponse>>().Subject;

        body.Success.Should().BeTrue();
        body.Error.Should().BeNull();
        body.Data.Should().BeSameAs(profile);
    }
}
