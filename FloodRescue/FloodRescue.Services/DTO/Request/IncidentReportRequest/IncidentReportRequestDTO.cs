using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.RescueMissionRequest
{
    public class IncidentReportRequestDTO
    {
        [Required(ErrorMessage = "RescueMissionID is required")]
        public Guid RescueMissionID { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        public double Longitude { get; set; }
    }
}