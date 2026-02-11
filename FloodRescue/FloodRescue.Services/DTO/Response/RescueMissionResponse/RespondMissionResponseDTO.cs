using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueMissionResponse
{
    public class RespondMissionResponseDTO
    {
        public Guid RescueMissionID { get; set; }
        public Guid RescueRequestID { get; set; }
        public string RequestShortCode { get; set; } = string.Empty;    
        public Guid RescueTeamID { get; set; }  

        //Status mới của rescue mission sau khi team phản hồi
        public string NewMissionStatus { get; set; } = string.Empty;  
        
        //Thời gian phản hồi của team
        public DateTime RespondedAt { get; set; }

        //Message thông báo kết quả
        public string Message { get; set; } = string.Empty; 
    }
}
