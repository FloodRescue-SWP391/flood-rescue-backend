using System;
using System.Collections.Generic;

namespace FloodRescue.Services.DTO.Request.IncidentReportRequest
{
    public class IncidentFilterDTO
    {
        public List<string>? Statuses { get; set; }
        public DateTime? CreatedFromDate { get; set; }
        public DateTime? CreatedToDate { get; set; }
        public DateTime? ResolvedFromDate { get; set; }
        public DateTime? ResolvedToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
