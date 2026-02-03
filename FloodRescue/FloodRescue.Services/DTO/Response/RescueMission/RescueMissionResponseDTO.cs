namespace FloodRescue.Services.DTO.Response.RescueMission
{
    public class RescueMissionResponseDTO
    {
        public Guid RescueMissionID { get; set; }
        public Guid RescueTeamID { get; set; }
        public Guid RescueRequestID { get; set; }
        public string Status { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}