using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.IncidentResponse
{
    /// <summary>
    /// DTO cho danh sách sự cố đang chờ xử lý (Pending)
    /// </summary>
    public class PendingIncidentResponseDTO
    {
        public Guid IncidentReportID { get; set; }
        public Guid RescueMissionID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
