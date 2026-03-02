using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueMissionResponse
{
    public class PendingMissionResponseDTO
    {
        public Guid RescueMissionID { get; set; }
        public DateTime AssignedAt { get; set; }
        public string MissionStatus { get; set; } = string.Empty;

        // Request Info
        public Guid RescueRequestID { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Citizen Info
        public string? CitizenName { get; set; }
        public string CitizenPhone { get; set; } = string.Empty;
        public int PeopleCount { get; set; }

        // Location Info
        public string? Address { get; set; }
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }

        // Images
        public List<string> ImageUrls { get; set; } = new List<string>();

        // Timestamps
        public DateTime RequestCreatedTime { get; set; }
    }
}
