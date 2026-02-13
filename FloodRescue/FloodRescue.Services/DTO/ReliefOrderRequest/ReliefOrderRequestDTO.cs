using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.ReliefOrderRequest
{
    public class ReliefOrderRequestDTO
    {
        public Guid RescueRequestId { get; set; }

        public Guid RescueTeamId { get; set; }
    }
}
