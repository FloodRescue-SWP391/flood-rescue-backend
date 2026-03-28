using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.ReliefOrder
{
    public class ReliefOrderResponseDTO
    {
        public Guid ReliefOrderID { get; set; }

        public Guid RescueRequestID { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedTime { get; set; }
        public List<ReliefItemDetailsTracking> ItemTrackings { get; set; } = new List<ReliefItemDetailsTracking>();

        
    }

    public class ReliefItemDetailsTracking
    {
        public int ReliefItemID { get; set; }
        public int Quantity { get; set; }
        public string WarehouseAddress { get; set; } = string.Empty;
    }
        
}
