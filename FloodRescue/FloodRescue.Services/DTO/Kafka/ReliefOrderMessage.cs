using System;

namespace FloodRescue.Services.DTO.Kafka
{
    public class ReliefOrderMessage
    {
        public Guid ReliefOrderID { get; set; }
        public Guid RescueRequestID { get; set; }
        public Guid RescueTeamID { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description {get; set;} = string.Empty;
        public DateTime CreatedTime { get; set; }
    }
}
