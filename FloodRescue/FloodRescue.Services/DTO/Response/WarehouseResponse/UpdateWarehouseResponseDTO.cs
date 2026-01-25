using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodRescue.Services.DTO.Response.Warehouse
{
    public class UpdateWarehouseResponseDTO
    {
        public int WarehouseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double LocationLong { get; set; }
        public double LocationLat { get; set; }
        public bool IsDeleted { get; set; }

    }
}