using System;

namespace FloodRescue.Services.DTO.SignalR
{
    public class IncidentResolvedNotification
    {
        public Guid IncidentReportID { get; set; }
        public Guid RescueMissionID { get; set; }
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? CoordinatorNote { get; set; }
        public string MissionStatus { get; set; } = string.Empty;
        public string TeamStatus { get; set; } = string.Empty;
        public DateTime? ResolvedTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
