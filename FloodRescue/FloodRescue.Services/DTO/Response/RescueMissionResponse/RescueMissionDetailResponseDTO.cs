using System;

namespace FloodRescue.Services.DTO.Response.RescueMissionResponse
{
    /// <summary>
    /// DTO chi tiết một nhiệm vụ cứu hộ
    /// </summary>
    public class RescueMissionDetailResponseDTO
    {
        public Guid RescueMissionID { get; set; }
        public Guid RescueTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // Thông tin nạn nhân
        public VictimInfoDTO RequestInfo { get; set; } = new VictimInfoDTO();
    }

    /// <summary>
    /// DTO thông tin nạn nhân (nested object)
    /// </summary>
    public class VictimInfoDTO
    {
        public Guid RescueRequestID { get; set; }
        public string? CitizenName { get; set; }
        public string CitizenPhone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }
        public int PeopleCount { get; set; }
        public string? Description { get; set; }
    }
}