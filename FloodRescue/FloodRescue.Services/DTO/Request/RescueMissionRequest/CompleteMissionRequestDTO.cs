using System;
using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.RescueMissionRequest
{
    public class CompleteMissionRequestDTO
    {
        [Required(ErrorMessage = "Rescue Mission ID is required")]
        public Guid RescueMissionID { get; set; }
    }
}
