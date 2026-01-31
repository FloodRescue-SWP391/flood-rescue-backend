using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.User
{
    public class UpdateUserRequestDTO
    {
        public string? Phone { get; set; }
        public string? FullName { get; set; }
        public string? RoleID { get; set; }
    }
}