using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.RescueMission
{
    public class CreateRescueMissionRequestDTO
    {
        [Required]
        public Guid RescueTeamID { get; set; }

        [Required]
        public Guid RescueRequestID { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        public DateTime? AssignedAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
