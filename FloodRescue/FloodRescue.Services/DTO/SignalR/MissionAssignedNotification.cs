using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.SignalR
{
    public class MissionAssignedNotification
    {
        //Header
        public string Title { get; set; } = "New Mission Assigned";
        public string NotificationType { get; set; } = "MissionAssigned";

        //Thông tin Mission
        public Guid MissionID { get; set; }
        public string MissionStatus { get; set; } = string.Empty;   
        public DateTime AssignedAt { get; set; }

        //Thông tin Rescue Request
        public string RequestShortCode { get; set; } = string.Empty;    
        public string? CitizenName { get; set; } = string.Empty;
        public string CitizenPhone { get; set; } = string.Empty;    
        public string? Address { get; set; } = string.Empty;
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }
        public double PeopleCount { get; set; }
        public string? Description { get; set; } = string.Empty;    

        //Action Required
        public string ActionMessage { get; set; } = "Please respond to the mission assignment in 5 minutes";    

    }
}
