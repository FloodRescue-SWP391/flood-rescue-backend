using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.WarehouseRequest
{
    public class WarehousesFilterRequestDTO
    {

        // Xài response là show warehouse response dto
        public string? Name { get; set; }
        public string? Address { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
