using System;

namespace FloodRescue.Services.DTO.SignalR
{
    public class ReliefOrderNotification
    {
        public Guid ReliefOrderID { get; set; }
        public Guid RescueRequestID { get; set; }
        public Guid RescueTeamID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
