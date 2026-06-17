namespace EAM.Api.Controllers;

using EAM.Application.Common;
using EAM.Application.DTOs.Audits;
using EAM.Application.DTOs.Payment.Response;
using EAM.Application.DTOs.Users;
using EAM.Application.DTOs.Users.Request;
using EAM.Application.DTOs.Users.Response;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserAccessor _currentUser;

    public UserController(
        IUserService userService,
        IAuditService auditService,
        IPaymentService paymentService,
        ICurrentUserAccessor currentUser)
    {
        _userService = userService;
        _auditService = auditService;
        _paymentService = paymentService;
        _currentUser = currentUser;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // E-Service Portal — Self-Profile
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// [E-Service Portal] Returns the profile of the currently authenticated user.
    /// <para>
    /// Security: the user ID is sourced exclusively from the validated JWT ‘sub’
    /// claim — there is no path parameter, making IDOR structurally impossible.
    /// A valid, non-expired JWT is required (<see cref="AuthorizeAttribute"/>).
    /// </para>
    /// </summary>
    /// <response code="200">Profile returned successfully.</response>
    /// <response code="401">No valid JWT present.</response>
    /// <response code="404">Authenticated user record not found (edge case).</response>
    [HttpGet("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> GetMyProfile()
    {
        // Require() throws AppException.Unauthorized (HTTP 401) if the token
        // is absent or invalid — defence-in-depth on top of [Authorize].
        var currentUser = _currentUser.Require();

        var profile = await _userService.GetMyProfileAsync(currentUser.UserId);
        return Ok(ApiResponse<UserProfileResponse>.Ok(profile));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Admin / Internal endpoints
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(ApiResponse<UserResponse>.Ok(user));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetPaged([FromQuery] PageRequest request)
    {
        var pagedUsers = await _userService.GetUsersAsync(request);
        return Ok(ApiResponse<PagedResult<UserResponse>>.Ok(pagedUsers));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] CreateUserRequest request)
    {
        var createdUser = await _userService.CreateUserAsync(request);
        return Ok(ApiResponse<UserResponse>.Ok(createdUser));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(Guid id, [FromBody] UpdateUserRequestDto request)
    {
        var updatedUser = await _userService.UpdateUserAsync(id, request);
        return Ok(ApiResponse<UserResponse>.Ok(updatedUser));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        return Ok(ApiResponse<bool>.Ok(true));
    }
    //US-7
    [HttpPost("manual-create")]
    //[Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> CreateManually([FromBody] ManualCreateUserRequestDto request)
    {
        var createdUser = await _userService.CreateUserManuallyAsync(request);

        return Ok(ApiResponse<UserResponse>.Ok(createdUser));
    }
    //US-8

    [HttpPut("{id:guid}/reopen")] 
    //[Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> ReopenAccount(Guid id, [FromBody] ReopenUserRequestDto request)
    {
        var updatedUser = await _userService.ReopenUserAccountAsync(id, request);
        return Ok(ApiResponse<UserResponse>.Ok(updatedUser));
    }
    //US-14-15
    [HttpGet("search")]
    //[Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> Search([FromQuery] UserSearchRequest request)
    {
        var result = await _userService.SearchUsersAsync(request);
        return Ok(ApiResponse<PagedResult<UserResponse>>.Ok(result));
    }
    //US16
    //For User Detail
    [HttpGet("detail/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetDetail(Guid id)
    {
        var detail = await _userService.GetUserDetailAsync(id);

        return Ok(ApiResponse<UserDetailResponse>.Ok(detail));
    }
    //US-16
    //For User Detail
    [HttpGet("{id:guid}/payments")]
    //[Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentResponse>>>> GetUserPayments(Guid id, [FromQuery] PageRequest request)
    {
        var payments = await _paymentService.GetPagedByUserIdAsync(id, request);

        return Ok(ApiResponse<PagedResult<PaymentResponse>>.Ok(payments));
    }
    //US-16
    [HttpGet("{id:guid}/audit-logs")]
    //[Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogResponse>>>> GetUserAuditLogs(Guid id, [FromQuery] PageRequest request)
    {

        var searchRequest = new AuditLogSearchRequest
        {
            Entityid = id,
            Page = request.Page,
            Size = request.Size
        };

        var logs = await _auditService.GetAuditLogsAsync(searchRequest);

        return Ok(ApiResponse<PagedResult<AuditLogResponse>>.Ok(logs));
    }

}