using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.Tracking
{
    public class UpdateLocationRequestDTO
    {
        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        [Required]
        public Guid RescueMissionID { get; set; }

        /// <summary>
        /// Thời điểm thiết bị thu thập GPS (không phải thời điểm server nhận)
        /// Giúp xử lý trường hợp mất mạng → gửi batch tọa độ cũ
        /// </summary>
        [Required]
        public DateTime ClientTimestamp { get; set; }
    }
}
