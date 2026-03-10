using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueRequestResponse
{
    public class RescueRequestListResponseDTO
    {
        public Guid RescueRequestID { get; set;  }
        public string ShortCode {get; set;} = string.Empty;
        public string CitizenName { get; set; } = string.Empty;
        public string CitizenPhone { get; set; } = string.Empty;
        public int PeopleCount { get; set; }
        public string? Address { get; set;  }
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }

    }
}
