using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueTeamResponse
{
    public class RescueTeamByIdResponseDTO
    {
        public List<TeamMemberDTO> TeamMember { get; set; } = new List<TeamMemberDTO>();
    }

}
