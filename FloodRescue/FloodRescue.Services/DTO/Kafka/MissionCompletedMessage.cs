using System;

namespace FloodRescue.Services.DTO.Kafka
{
    public class MissionCompletedMessage
    {
        public Guid RescueMissionID { get; set; }
        public Guid RescueRequestID { get; set; }
        public string RequestShortCode { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string MissionStatus { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public DateTime? EndTime { get; set; }
        public Guid? CoordinatorID { get; set; }
    }
}
