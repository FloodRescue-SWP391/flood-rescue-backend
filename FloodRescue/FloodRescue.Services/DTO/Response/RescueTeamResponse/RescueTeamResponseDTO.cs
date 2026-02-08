using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueTeamResponse
{
    public class RescueTeamResponseDTO
    {
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
    }
}
