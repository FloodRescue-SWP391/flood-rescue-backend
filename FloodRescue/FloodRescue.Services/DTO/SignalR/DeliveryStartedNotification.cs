using System;

namespace FloodRescue.Services.DTO.SignalR
{
    public class DeliveryStartedNotification
    {
        public Guid ReliefOrderID { get; set; }
        public Guid RescueMissionID { get; set; }
        public Guid RescueRequestID { get; set; }
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime? PickedUpTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
