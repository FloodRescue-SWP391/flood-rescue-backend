using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.RescueMissionRequest
{
    public class DispatchMissionRequestDTO
    {
        /// <summary>
        /// ID của RescueRequest cần assign (đơn cứu hộ)
        /// </summary>
        public Guid RescueRequestID { get; set; }
        /// <summary>
        /// Id của RescueTeam được chọn để làm nhiệm vụ đó
        /// </summary>
        public Guid RescueTeamID { get; set; }

    }
}
