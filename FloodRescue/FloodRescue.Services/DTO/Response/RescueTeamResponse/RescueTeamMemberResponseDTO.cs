using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueTeamResponse
{
    public class RescueTeamMemberResponseDTO
    {
        public Guid UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsLeader { get; set; }
    }
}
