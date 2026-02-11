using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Kafka
{
    public class MissionAssignedMessage
    {
        //Thông tin Mission
        public Guid MissionID { get; set; }
        public DateTime AssignedAt { get; set; }
        public string MissionStatus {get; set;} = string.Empty;

        //Thông tin Request
        public Guid RescueRequestID { get; set; }
        public string RequestShortCode { get; set; } = string.Empty;    
        public string CitizenPhone { get; set; } = string.Empty;    
        public string? CitizenName { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;    
        public double LocationLatitude { get; set; }    
        public double LocationLongitude { get; set; }
        public double PeopleCount { get; set; }
        public string? Description {get; set;} = string.Empty;

        //Thông tin Team
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;

    }
}
