using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.IncidentReport
{
    public class CreateIncidentReportRequestDTO
    {
        [Required]
        public Guid RescueMissionID { get; set; }

        [Required]
        public Guid ReportedID { get; set; }

        [Required]
        public Guid ResolvedBy { get; set; }

        public DateTime? ResolvedTime { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public double Latitiude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? CoordinatorNote { get; set; }
    }
}