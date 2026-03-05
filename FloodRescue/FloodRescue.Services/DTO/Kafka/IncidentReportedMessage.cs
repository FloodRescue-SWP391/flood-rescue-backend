namespace FloodRescue.Services.DTO.Kafka
{
    public class IncidentReportedMessage
    {
        public Guid IncidentReportID { get; set; }
        public Guid RescueMissionID { get; set; }
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public Guid ReportedID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string IncidentStatus { get; set; } = string.Empty;
        public string MissionStatus { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
    }
}
