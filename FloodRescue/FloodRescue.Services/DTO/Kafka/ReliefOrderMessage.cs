using System;

namespace FloodRescue.Services.DTO.Kafka
{
    public class ReliefOrderMessage
    {
        public Guid ReliefOrderId { get; set; }
        public Guid RescueRequestId { get; set; }
        public Guid RescueTeamId { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedTime { get; set; }
    }
}
