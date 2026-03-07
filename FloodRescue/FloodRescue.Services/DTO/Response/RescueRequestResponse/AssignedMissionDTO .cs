using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueRequestResponse
{
    public class AssignedMissionDTO
    {
        public Guid MissionID { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
