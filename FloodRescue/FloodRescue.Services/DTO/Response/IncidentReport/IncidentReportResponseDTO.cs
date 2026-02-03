namespace FloodRescue.Services.DTO.Response.IncidentReport
{
    public class IncidentReportResponseDTO
    {
        public Guid IncidentReportID { get; set; }
        public Guid RescueMissionID { get; set; }
        public Guid ReportedID { get; set; }
        public Guid ResolvedBy { get; set; }

        public DateTime? ResolvedTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public double Latitiude { get; set; }
        public double Longitude { get; set; }

        public DateTime CreatedTime { get; set; }
        public string Status { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? CoordinatorNote { get; set; }
    }
}