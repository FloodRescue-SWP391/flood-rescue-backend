using FloodRescue.Services.DTO.Kafka;
using System;

namespace FloodRescue.Services.DTO.SignalR
{
    public class OrderPreparedNotification
    {
        public Guid ReliefOrderID { get; set; }
        public Guid RescueRequestID { get; set; }
        public Guid? RescueTeamID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PreparedTime { get; set; }
        public List<OrderPreparedMessage> OrderMessages { get; set; } = new List<OrderPreparedMessage>();
        public string Message { get; set; } = string.Empty;
    }

}
