using System;
using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.RescueMissionRequest
{
    public class ConfirmPickUpRequestDTO
    {
        [Required(ErrorMessage = "Rescue Mission ID is required")]
        public Guid RescueMissionID { get; set; }

        [Required(ErrorMessage = "Relief Order ID is required")]
        public Guid ReliefOrderID { get; set; }
    }
}
