using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.SignalR
{
    public class TeamRejectedNotification
    {
        public string Title { get; set; } = string.Empty;       
        public string NotificationType { get; set; } = string.Empty;    
        public Guid RescueMissionID { get; set; }   
        public Guid RescueRequestID { get; set; }
        public string RequestShortCode { get; set; } = string.Empty;
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;    
        public string? RejectReason { get; set; } = string.Empty;   
        public DateTime RejectedAt { get; set; }

        //Hiển thị ra Coordinator cần assign team khác
        public string ActionMessage { get; set; } = string.Empty;


    }
}
