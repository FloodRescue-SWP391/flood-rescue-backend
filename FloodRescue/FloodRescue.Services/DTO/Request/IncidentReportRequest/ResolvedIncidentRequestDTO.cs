using System;
using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.IncidentReportRequest
{
    public class ResolvedIncidentRequestDTO
    {
        [Required(ErrorMessage = "IncidentReportID is required")]
        public Guid IncidentReportID { get; set; }

        public string? CoordinatorNote { get; set; }
    }
}
