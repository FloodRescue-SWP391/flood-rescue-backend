using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueRequestResponse
{
    public class CreateRescueRequestResponseDTO
    {
        public Guid RescueRequestID { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CitizenPhone { get; set; } = string.Empty;
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedTime { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}
