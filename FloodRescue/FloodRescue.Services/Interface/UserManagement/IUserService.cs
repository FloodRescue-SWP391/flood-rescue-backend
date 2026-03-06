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
    }
}
