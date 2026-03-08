using FloodRescue.Services.DTO.Request.UserRequest;
using FloodRescue.Services.DTO.Response.UserResponse;

namespace FloodRescue.Services.Interface.UserManagement
{
    public interface IUserService
    {
        /// <summary>
        /// Admin cập nhật thông tin nhân sự hệ thống (FullName, Phone, RoleID)
        /// </summary>
        Task<(UpdateUserResponseDTO? Data, string? ErrorMessage)> UpdateSystemUserAsync(Guid userId, UpdateUserRequestDTO request);

        /// <summary>
        /// Admin khóa tài khoản nhân sự (Soft Delete - IsDeleted = true)
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> DeactivateUserAsync(Guid userId, Guid currentUserId);
    }
}
