using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.Auth
{
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8,ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(25)]
        public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        [MaxLength(15)]
        public string Phone { get; set; } = string.Empty;
        [Required(ErrorMessage = "FullName is required")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "RoleID is required")]
        [MaxLength(2)]
        [RegularExpression("^(RC|IM|RT|AD)$", ErrorMessage = "RoleID must be AD, RC, IM or RT")]
        public string RoleID { get; set; } = string.Empty;
    }
}
