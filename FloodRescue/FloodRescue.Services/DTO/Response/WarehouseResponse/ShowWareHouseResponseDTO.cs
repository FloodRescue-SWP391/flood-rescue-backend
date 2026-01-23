using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.Warehouse
{
    public class ShowWareHouseResponseDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double LocationLong { get; set; }
        public double LocationLat { get; set; }
        public string ManagedBy { get; set; } = string.Empty;
    }
}
