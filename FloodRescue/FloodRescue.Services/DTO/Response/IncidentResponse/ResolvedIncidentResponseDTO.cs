using System;

namespace FloodRescue.Services.DTO.Response.IncidentResponse
{
    public class ResolvedIncidentResponseDTO
    {
        public Guid IncidentReportID { get; set; }
        public Guid RescueMissionID { get; set; }
        public string IncidentStatus { get; set; } = string.Empty;
        public string? CoordinatorNote { get; set; }
        public Guid ResolvedBy { get; set; }
        public DateTime? ResolvedTime { get; set; }
        public string MissionStatus { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string TeamStatus { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
