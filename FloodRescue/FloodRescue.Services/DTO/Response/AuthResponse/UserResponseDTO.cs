using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.AuthResponse
{
    public class UserResponseDTO
    {
        public Guid UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleID { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
