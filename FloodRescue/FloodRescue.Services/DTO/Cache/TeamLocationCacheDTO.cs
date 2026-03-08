using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Cache
{
    public class TeamLocationCacheDTO
    {
        public Guid TeamID { get; set; }
        public Guid RescueMission { get; set; }
        public double Latitude { get; set; }    
        public double Longitude { get; set; }
        public DateTime ClientTimeStamp { get; set; }
    }
}
