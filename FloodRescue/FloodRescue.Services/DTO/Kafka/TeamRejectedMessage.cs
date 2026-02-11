using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Kafka
{
    public class TeamRejectedMessage
    {
        public Guid RescueMissionID { get; set; }
        public string MissionStatus { get; set; } = string.Empty;
        public DateTime RejectedAt { get; set; }
        public string? RejectReason { get; set; } = string.Empty;   
        public Guid RescueRequestID { get; set; }
        public string RequestShortCode { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public Guid? CoordinatorID { get; set; }    
    }
}
