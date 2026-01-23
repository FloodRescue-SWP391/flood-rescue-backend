using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.WarehouseRequest
{
    public class UpdateWarehouseRequestDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double LocationLong { get; set; }
        public double LocationLat { get; set; }
        public bool IsDeleted { get; set; }
    }
}
