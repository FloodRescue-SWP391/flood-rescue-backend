using System;

namespace FloodRescue.Services.DTO.Response.RescueMissionResponse
{
    public class CompleteMissionResponseDTO
    {
        public Guid RescueMissionID { get; set; }
        public Guid RescueRequestID { get; set; }
        public string RequestShortCode { get; set; } = string.Empty;
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string NewMissionStatus { get; set; } = string.Empty;
        public string NewRequestStatus { get; set; } = string.Empty;
        public string NewTeamStatus { get; set; } = string.Empty;
        public DateTime? EndTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
