using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Kafka
{
    public class TeamAcceptedMessage
    {
        public Guid RescueMissionID { get; set; }
        public string MissionStatus { get; set; } = string.Empty;   
        public DateTime AcceptedAt { get; set; }

        public Guid RescueRequestID { get; set; }
        public string RequestShortCode { get; set; } = string.Empty;    
        public string? CitizenName { get; set; } = string.Empty;
        public string CitizenPhone { get; set; } = string.Empty;    
        public string? Address { get; set; } = string.Empty;    
        public double LocationLatitude { get; set; }    
        public double LocationLongitude { get; set; }
        public int PeopleCount { get; set; }

        public Guid RescueTeamID { get; set; }  
        public string TeamName { get; set; } = string.Empty;
        public Guid? CoordinatorID { get; set; }


    }
}
