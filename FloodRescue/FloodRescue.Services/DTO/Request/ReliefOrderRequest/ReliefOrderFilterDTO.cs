using System;
using System.Collections.Generic;

namespace FloodRescue.Services.DTO.Request.ReliefOrderRequest
{
    public class ReliefOrderFilterDTO
    {
        public List<string>? Statuses { get; set; }
        public DateTime? CreatedFromDate { get; set; }
        public DateTime? CreatedToDate { get; set; }
        public DateTime? PreparedFromDate { get; set; }
        public DateTime? PreparedToDate { get; set; }
        public DateTime? PickedUpFromDate { get; set; }
        public DateTime? PickedUpToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
