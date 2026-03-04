using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.RescueMissionRequest
{
    public class RescueMissionFilterDTO
    {
        public List<string>? Statuses { get; set; }
        public Guid? RescueTeamID { get; set; }
        public int PageNumber { get; set; } = 1;
        public DateTime? AssignedFromDate { get; set; }
        public DateTime? AssignedToDate { get; set; }   
        public DateTime? StartFromDate { get; set; }
        public DateTime? StartToDate { get; set; }
        public DateTime? EndFromDate { get; set; }
        public DateTime? EndToDate { get; set; }
        public int PageSize { get; set; } = 10;



    }
}
