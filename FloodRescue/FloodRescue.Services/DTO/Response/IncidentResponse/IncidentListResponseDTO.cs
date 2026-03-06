using System;

namespace FloodRescue.Services.DTO.Response.IncidentResponse
{
    public class IncidentListResponseDTO
    {
        public Guid IncidentReportID { get; set; }
        public Guid RescueMissionID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
    }
}
