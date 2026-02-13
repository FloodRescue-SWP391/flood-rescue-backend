using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.RescueRequest
{
    /// <summary>
    /// DTO đóng gói message gửi lên Kafka topic sau khi tạo RescueRequest thành công
    /// Consumer sẽ parse message này ra để xử lí (SMS, notification, ...)
    /// </summary>
    public class RescueRequestKafkaMessage
    {
        public Guid RescueRequestID { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string CitizenPhone { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }
        public int PeopleCount { get; set; }    
        public DateTime CreatedTime { get; set; }
    }
}
