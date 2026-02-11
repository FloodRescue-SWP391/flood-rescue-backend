using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.RescueMissionRequest
{
    public class RespondMessageRequestDTO
    {
        [Required(ErrorMessage = "Rescue Mission ID is required")]
        public Guid RescueMissionID { get; set; }
        [Required(ErrorMessage = "IsAccepted is required")]
        public bool IsAccepted { get; set; }
        public string? RejectReason { get; set; }   

    }
}
