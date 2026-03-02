using System;

namespace FloodRescue.Services.DTO.SignalR
{
    public class MissionCompletedNotification
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
        public string Message { get; set; } = string.Empty;
    }
}
