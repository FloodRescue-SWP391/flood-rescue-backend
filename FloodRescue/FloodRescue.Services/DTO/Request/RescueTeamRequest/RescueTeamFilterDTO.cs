using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.RescueTeamRequest
{
    public class RescueTeamFilterDTO
    {
        public List<string>? Status { get; set; }
        public string? TeamName { get; set;  }
        public string? City { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
