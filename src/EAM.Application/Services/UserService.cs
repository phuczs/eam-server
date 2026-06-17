

using AutoMapper;
using EAM.Application.Common;
using EAM.Application.DTOs.Users;
using EAM.Application.DTOs.Users.Request;
using EAM.Application.DTOs.Users.Response;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Repositories;
using EAM.Application.Interfaces.Services;
using EAM.Domain.Entities;
using System.Text.Json;

namespace EAM.Application.Services {

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserAccessor _currentUser;
        private readonly IAuditRepository _auditLogRepository;
        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            ICurrentUserAccessor currentUser,
            IAuditRepository auditLogRepository)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _currentUser = currentUser;
            _auditLogRepository = auditLogRepository;
        }
        //US-15
        public async Task<UserDetailResponse> GetUserDetailAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
            {
                throw AppException.NotFound("User account", id);
            }

            return _mapper.Map<UserDetailResponse>(user);
        }

        /// <summary>
        /// E-Service Portal — "View My Profile".
        /// The <paramref name="userId"/> MUST originate from the caller's own JWT claim;
        /// this method trusts it and never validates role access against it.
        /// </summary>
        public async Task<UserProfileResponse> GetMyProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                // Return NotFound rather than Forbidden so we don't leak
                // information about whether other users exist.
                throw AppException.NotFound("User profile", userId);
            }

            return _mapper.Map<UserProfileResponse>(user);
        }
        //US-14 and US-15 search name or NRIC with filter
        public async Task<PagedResult<UserResponse>> SearchUsersAsync(UserSearchRequest request)
        {
            var (items, total) = await _userRepository.SearchPagedAsync(
                    request.Keyword,
                    request.Status,
                    request.MinAge,
                    request.MaxAge,
                    request.Skip,
                    request.Size);

            return new PagedResult<UserResponse>
            {
                Items = _mapper.Map<IReadOnlyList<UserResponse>>(items),
                Total = total,
                Page = request.Page,
                Size = request.Size
            };
        }
        //US-8 Create User
        public async Task<UserResponse> CreateUserManuallyAsync(ManualCreateUserRequestDto request)
        {
            var currentUser = _currentUser.Require();
            //NRIC check
            var isExists = await _userRepository.IsOfficialIdExistsAsync(request.OfficialId);
            if (isExists)
            {
                throw AppException.Conflict($"An account with NRIC '{request.OfficialId}' already exists in the system.");
            }

            User createdUser = null!;

            await _userRepository.ExecuteInTransactionAsync(async () =>
            {
                var newUser = new User
                {
                    OfficialId = request.OfficialId,
                    AccountActivatedAt = request.AccountStartDate,

                    Status = "active",
                    AccountStatus = "active",

                    AccountCreatedByUserId = currentUser.UserId,
                    CreatedAt = DateTime.UtcNow,
                    //avoid age validation
                    IsExemptFromAutomatedRules = true
                };

                createdUser = await _userRepository.AddAsync(newUser);

                //Auditlog
                var metadata = new
                {
                    ExceptionType = request.ExceptionType,
                    Justification = request.OverrideJustification
                };

                var auditLog = new AuditLog
                {
                    ActorUserId = currentUser.UserId,
                    Action = "ManualCreateException",
                    EntityType = "User",
                    EntityId = createdUser.Id,
                    MetadataJson = JsonSerializer.Serialize(metadata), //Exception Reason
                    CreatedAt = DateTime.UtcNow 
                };

                await _auditLogRepository.AddAsync(auditLog);
            });

            return _mapper.Map<UserResponse>(createdUser);
        }
        public async Task<UserResponse> ReopenUserAccountAsync(Guid userId, ReopenUserRequestDto request)
        {
            // 
            var currentUser = _currentUser.Require();
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw AppException.NotFound("User", userId);
            }
            //Only open the account close
            if (user.AccountStatus?.ToLower() != "closed")
            {
                throw AppException.Conflict($"Cannot reopen account. Current status is '{user.AccountStatus}'. Account must be 'CLOSED'.");
            }

            // Transaction
            await _userRepository.ExecuteInTransactionAsync(async () =>
            {
                user.Status = "active";
                user.AccountStatus = "active";
                //reset fields when closed the account
                user.AccountClosedAt = null;
                user.AccountClosureReason = null;
                user.AccountPendingClosureAt = null;

                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                var metadata = new
                {
                    Justification = request.Justification,
                    PreviousStatus = "closed",
                    NewStatus = "active"
                };

                var auditLog = new AuditLog
                {
                    ActorUserId = currentUser.UserId,
                    Action = "ReopenAccount",
                    EntityType = "User",
                    EntityId = user.Id,
                    MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata),
                    CreatedAt = DateTime.UtcNow
                };

                await _auditLogRepository.AddAsync(auditLog);
            });

            return _mapper.Map<UserResponse>(user);
        }
        public async Task<UserResponse> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null) throw AppException.NotFound("User", id);

            return _mapper.Map<UserResponse>(user);
        }

        public async Task<PagedResult<UserResponse>> GetUsersAsync(PageRequest request)
        {
            var (items, total) = await _userRepository.GetPagedAsync(request.Skip, request.Size);

            return new PagedResult<UserResponse>
            {
                Items = _mapper.Map<IReadOnlyList<UserResponse>>(items),
                Total = total,
                Page = request.Page,
                Size = request.Size
            };
        }

        public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
        {
            var currentUser = _currentUser.Require();
            var user = _mapper.Map<User>(request);

            user.AccountCreatedByUserId = currentUser.UserId;

            var createdUser = await _userRepository.AddAsync(user);
            return _mapper.Map<UserResponse>(createdUser);
        }

        public async Task<UserResponse> UpdateUserAsync(Guid id, UpdateUserRequestDto request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw AppException.NotFound("User", id);

            _mapper.Map(request, user);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return _mapper.Map<UserResponse>(user);
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw AppException.NotFound("User", id);

            await _userRepository.DeleteAsync(user);
        }
    }
}
