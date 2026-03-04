using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueMissionResponse
{
    public class RescueMissionListResponseDTO
    {
        public Guid RescueMissionID { get; set; }
        public Guid RescueTeamID { get; set; }
        public Guid RescueRequestID { get; set; }
        public string Status { get; set;  } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? CitizenAddress { get; set; }

    }
}
