using EAM.Application.Common;
using EAM.Application.DTOs.Users;
using EAM.Application.DTOs.Users.Request;
using EAM.Application.DTOs.Users.Response;

namespace EAM.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserResponse> GetUserByIdAsync(Guid id);
    Task<PagedResult<UserResponse>> GetUsersAsync(PageRequest request);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request);
    Task<UserResponse> UpdateUserAsync(Guid id, UpdateUserRequestDto request);
    Task DeleteUserAsync(Guid id);
    //US7-Hugo
    Task<UserResponse> CreateUserManuallyAsync(ManualCreateUserRequestDto request);
    //US-8
    Task<UserResponse> ReopenUserAccountAsync(Guid userId, ReopenUserRequestDto request);
    //US-14
    Task<PagedResult<UserResponse>> SearchUsersAsync(UserSearchRequest request);
    //US-15
    Task<UserDetailResponse> GetUserDetailAsync(Guid id);

    /// <summary>
    /// E-Service Portal: returns the authenticated user's own profile.
    /// The caller is responsible for passing the user ID extracted from
    /// the JWT — this method performs no claim resolution itself.
    /// </summary>
    Task<UserProfileResponse> GetMyProfileAsync(Guid userId);

}