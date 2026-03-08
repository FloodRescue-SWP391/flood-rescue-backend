using System;

namespace FloodRescue.Services.DTO.Response.UserResponse
{
    public class UpdateUserResponseDTO
    {
        public Guid UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RoleID { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
