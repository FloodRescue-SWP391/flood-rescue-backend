using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.ReliefOrder
{
    public class ReliefOrderResponseDTO
    {
        public Guid ReliefOrderId { get; set; }

        public Guid RescueRequestId { get; set; }

        public Guid ManagerId { get; set; }

        public int WarehouseId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedTime { get; set; }
    }
}
