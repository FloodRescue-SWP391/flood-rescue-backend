using FloodRescue.Repositories.Interface;
using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.UserRequest;
using FloodRescue.Services.DTO.Response.UserResponse;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using UserEntity = FloodRescue.Repositories.Entites.User;
using RoleEntity = FloodRescue.Repositories.Entites.Role;

namespace FloodRescue.Services.Implements.UserManagement
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;
        private readonly ICacheService _cacheService;

        private const string USER_FILTER_PREFIX = "user:filter:";

        public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<(UpdateUserResponseDTO? Data, string? ErrorMessage)> UpdateSystemUserAsync(Guid userId, UpdateUserRequestDTO request)
        {
            _logger.LogInformation("[UserService] UpdateSystemUser called for UserID: {UserID}", userId);

            // Tìm User trong DB theo ID
            UserEntity? user = await _unitOfWork.Users.GetAsync(
                (UserEntity u) => u.UserID == userId && !u.IsDeleted,
                u => u.Role!);

            if (user == null)
            {
                _logger.LogWarning("[UserService - Sql Server] User with ID: {UserID} not found", userId);
                return (null, "Không tìm thấy nhân sự này.");
            }

            // Không cho phép cập nhật tài khoản Citizen (RoleID không phải AD/RC/IM/RT nội bộ)
            if (user.RoleID != "AD" && user.RoleID != "RC" && user.RoleID != "IM" && user.RoleID != "RT")
            {
                _logger.LogWarning("[UserService] Attempted to update non-system user. UserID: {UserID}, RoleID: {RoleID}", userId, user.RoleID);
                return (null, "Không thể cập nhật tài khoản người dân.");
            }

            // Không cho phép Admin sửa tài khoản Admin khác
            if (user.RoleID == "AD")
            {
                _logger.LogWarning("[UserService] Attempted to update Admin account. UserID: {UserID}", userId);
                return (null, "Không thể cập nhật tài khoản Admin.");
            }

            // Xử lý RoleID: nếu != null thì validate và update
            string newRoleID = user.RoleID;
            if (!string.IsNullOrWhiteSpace(request.RoleID))
            {
                RoleEntity? role = await _unitOfWork.Roles.GetAsync((RoleEntity r) => r.RoleID == request.RoleID && !r.IsDeleted);

                if (role == null)
                {
                    _logger.LogWarning("[UserService - Sql Server] Role with ID: {RoleID} not found", request.RoleID);
                    return (null, $"RoleID '{request.RoleID}' không tồn tại trong hệ thống.");
                }

                newRoleID = request.RoleID;
            }

            // Xử lý Phone: nếu != null thì validate trùng và update
            if (!string.IsNullOrWhiteSpace(request.Phone) && user.Phone != request.Phone)
            {
                UserEntity? existingPhone = await _unitOfWork.Users.GetAsync(
                    (UserEntity u) => u.Phone == request.Phone && u.UserID != userId && !u.IsDeleted);

                if (existingPhone != null)
                {
                    _logger.LogWarning("[UserService - Sql Server] Phone {Phone} already exists for another user", request.Phone);
                    return (null, $"Số điện thoại '{request.Phone}' đã được sử dụng bởi người dùng khác.");
                }
            }

            _logger.LogInformation("[UserService] Updating user. UserID: {UserID}, FullName: {OldName} -> {NewName}, Role: {OldRole} -> {NewRole}, Phone: {OldPhone} -> {NewPhone}",
                userId,
                user.FullName, request.FullName ?? user.FullName,
                user.RoleID, newRoleID,
                user.Phone, request.Phone ?? user.Phone);

            // Cập nhật thông tin - chỉ update field != null, giữ nguyên field == null
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone;

            user.RoleID = newRoleID;

            int saveResult = await _unitOfWork.SaveChangesAsync();

            if (saveResult <= 0)
            {
                _logger.LogError("[UserService - Error] SaveChanges returned 0 rows during update user. UserID: {UserID}", userId);
                return (null, "Cập nhật thông tin nhân sự thất bại.");
            }

            _logger.LogInformation("[UserService] Successfully updated user. UserID: {UserID}", userId);


            await _cacheService.RemovePatternAsync($"{USER_FILTER_PREFIX}");

            _logger.LogInformation("[UserService - Redis] Cleared cached with cache key pattern {key}", USER_FILTER_PREFIX);

            // Lấy lại Role name để trả response (có thể đã thay đổi hoặc giữ nguyên)
            RoleEntity? currentRole = await _unitOfWork.Roles.GetAsync((RoleEntity r) => r.RoleID == user.RoleID);

            // Tạo response
            UpdateUserResponseDTO response = new()
            {
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                Phone = user.Phone,
                RoleID = user.RoleID,
                RoleName = currentRole?.RoleName ?? string.Empty,
                Message = "Cập nhật thông tin nhân sự thành công."
            };

            return (response, null);
        }


        /// <summary>
        /// Hàm này là hàm xóa User
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        public async Task<(bool Success, string? ErrorMessage)> DeactivateUserAsync(Guid userId, Guid currentUserId)
        {
            _logger.LogInformation("[UserService] DeactivateUser called for UserID: {UserID} by AdminID: {AdminID}", userId, currentUserId);

            // Critical Check: Không cho phép Admin tự khóa chính tài khoản của mình
            if (currentUserId == userId)
            {
                _logger.LogWarning("[UserService] Admin {AdminID} attempted to deactivate their own account", currentUserId);
                return (false, "Bạn không thể tự khóa tài khoản của chính mình.");
            }

            // Tìm User trong DB theo ID
            UserEntity? user = await _unitOfWork.Users.GetAsync(
                (UserEntity u) => u.UserID == userId && !u.IsDeleted);

            if (user == null)
            {
                _logger.LogWarning("[UserService - Sql Server] User with ID: {UserID} not found or already deactivated", userId);
                return (false, "Không tìm thấy nhân sự này.");
            }

            // Không cho phép khóa tài khoản Admin khác
            if (user.RoleID == "AD")
            {
                _logger.LogWarning("[UserService] Attempted to deactivate Admin account. UserID: {UserID}", userId);
                return (false, "Không thể khóa tài khoản Admin.");
            }

            _logger.LogInformation("[UserService] Deactivating user. UserID: {UserID}, Username: {Username}, Role: {RoleID}",
                userId, user.Username, user.RoleID);

            // Soft Delete: Gán IsDeleted = true
            user.IsDeleted = true;

            int saveResult = await _unitOfWork.SaveChangesAsync();

            if (saveResult <= 0)
            {
                _logger.LogError("[UserService - Error] SaveChanges returned 0 rows during deactivate user. UserID: {UserID}", userId);
                return (false, "Khóa tài khoản nhân sự thất bại.");
            }

            _logger.LogInformation("[UserService] Successfully deactivated user. UserID: {UserID}, Username: {Username}", userId, user.Username);

            await _cacheService.RemovePatternAsync($"{USER_FILTER_PREFIX}");

            _logger.LogInformation("[UserService - Redis] Cleared cached with cache key pattern {key}", USER_FILTER_PREFIX);

          
            return (true, null);
        }

        public async Task<PagedResult<UserListResponseDTO>> GetFilteredUsersAsync(UserFilterDTO filter)
        {
            _logger.LogInformation("[UserService] GetFilteredUsers called. Keyword: {Keyword}, RoleID: {RoleID}, IsActive: {IsActive}, Page: {Page}, Size: {Size}",
                filter.SearchKeyword ?? "None", filter.RoleID ?? "All",
                filter.IsActive?.ToString() ?? "All", filter.PageNumber, filter.PageSize);

            // Check cache
            string cacheKey = BuildUserFilterCacheKey(filter);

            PagedResult<UserListResponseDTO>? cached = await _cacheService.GetAsync<PagedResult<UserListResponseDTO>>(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("[UserService - Redis] Cache hit for filter key: {Key}. TotalCount: {Count}", cacheKey, cached.TotalCount);
                return cached;
            }

            _logger.LogInformation("[UserService - Redis] Cache miss for filter key: {Key}. Querying database.", cacheKey);

            // Khởi tạo query - chỉ lấy nhân sự nội bộ (AD, RC, IM, RT), loại trừ Citizen
            string[] systemRoles = { "AD", "RC", "IM", "RT" };
            IQueryable<UserEntity> query = _unitOfWork.Users.GetQueryable()
                .Where(u => systemRoles.Contains(u.RoleID));

            // Lọc theo SearchKeyword (FullName hoặc Phone)
            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                string keyword = filter.SearchKeyword.Trim();
                query = query.Where(u => u.FullName.Contains(keyword) || u.Phone.Contains(keyword));
            }

            // Lọc theo RoleID
            if (!string.IsNullOrWhiteSpace(filter.RoleID))
            {
                query = query.Where(u => u.RoleID == filter.RoleID);
            }

            // Lọc theo trạng thái (IsActive = !IsDeleted)
            if (filter.IsActive.HasValue)
            {
                bool isDeleted = !filter.IsActive.Value;
                query = query.Where(u => u.IsDeleted == isDeleted);
            }

            // Tính tổng số dòng cho FE làm thanh phân trang
            int totalCount = await query.CountAsync();

            _logger.LogInformation("[UserService - Sql Server] Total {Count} user(s) matched filter", totalCount);

            // Sắp xếp theo FullName + Include Role + phân trang
            List<UserEntity> entities = await query
                .OrderBy(u => u.FullName)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Include(u => u.Role!)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("[UserService - Sql Server] Retrieved {Count} user(s) for current page", entities.Count);

            // Mapping sang DTO
            List<UserListResponseDTO> dtos = entities.Select(u => new UserListResponseDTO
            {
                UserID = u.UserID,
                Username = u.Username,
                FullName = u.FullName,
                Phone = u.Phone,
                RoleID = u.RoleID,
                RoleName = u.Role?.RoleName ?? string.Empty,
                IsActive = !u.IsDeleted
            }).ToList();

            // Đóng gói và cache
            PagedResult<UserListResponseDTO> result = new()
            {
                Data = dtos,
                TotalCount = totalCount
            };

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            _logger.LogInformation("[UserService - Redis] Cached filter result. Key: {Key}, DataCount: {Count}, TotalCount: {Total}", cacheKey, dtos.Count, totalCount);

            return result;
        }

        private string BuildUserFilterCacheKey(UserFilterDTO filter)
        {
            return $"{USER_FILTER_PREFIX}kw={filter.SearchKeyword ?? ""}" +
                   $"|r={filter.RoleID ?? ""}" +
                   $"|a={filter.IsActive}" +
                   $"|p={filter.PageNumber}|ps={filter.PageSize}";
        }
    }
}
