using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueRequestResponse
{
    public class RescueRequestDetailResponseDTO
    {
        public Guid RescueRequestID { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string? CitizenName { get; set; }
        public string CitizenPhone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;  
        public string? Description { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public string? RejectedNote { get; set; }
        public DateTime CreatedTime { get; set; }

        public List<AssignedMissionDTO> AssignedMissions { get; set; } = new();
    }
}
