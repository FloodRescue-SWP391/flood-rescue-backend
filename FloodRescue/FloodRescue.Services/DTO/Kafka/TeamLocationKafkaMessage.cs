using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Kafka
{
    public class TeamLocationKafkaMessage
    {
        public Guid TeamID { get; set; }    
        public Guid RescueMissionID { get; set; }   
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime ClientTimestamp { get; set; }
    }
}
