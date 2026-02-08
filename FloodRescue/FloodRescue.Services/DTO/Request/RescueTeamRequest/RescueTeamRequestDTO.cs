using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.RescueTeamRequest
{
    public class RescueTeamRequestDTO
    {
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
    }
}
