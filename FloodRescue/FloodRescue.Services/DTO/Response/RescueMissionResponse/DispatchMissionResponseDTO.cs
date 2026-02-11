using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueMissionResponse
{
    //DTO sẽ hiển thị trả về cho Coordinator - màn hình của Coordinator
    public class DispatchMissionResponseDTO
    {
        public Guid RescueMissionID { get; set; }
        public Guid RescueRequestID { get; set; }
        public string RequestShortCode { get; set; } = string.Empty;
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;    
        public string MissionStatus { get; set; } = string.Empty;   
        public DateTime AssignedAt { get; set; }
        public string Message { get; set; } = string.Empty; 
    }
}
