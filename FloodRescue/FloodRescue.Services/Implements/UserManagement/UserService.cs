using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.UserRequest;
using FloodRescue.Services.DTO.Response.UserResponse;
using FloodRescue.Services.Interface.UserManagement;
using Microsoft.Extensions.Logging;

using UserEntity = FloodRescue.Repositories.Entites.User;
using RoleEntity = FloodRescue.Repositories.Entites.Role;

namespace FloodRescue.Services.Implements.UserManagement
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
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

            return (true, null);
        }
    }
}
