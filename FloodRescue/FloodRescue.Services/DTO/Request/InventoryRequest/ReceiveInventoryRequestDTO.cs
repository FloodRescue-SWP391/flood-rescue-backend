using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.InventoryRequest
{
    /// <summary>
    /// DTO cho từng item cần nhập kho
    /// </summary>
    public class InventoryItemDTO 
    {
        [Required(ErrorMessage = "ReliefItemID is required")]
        public int ReliefItemID { get; set; }
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1,int.MaxValue,ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
    }

    public class ReceiveInventoryRequestDTO
    {
        [Required(ErrorMessage = "WarehouseID is required")]
        public int WarehouseID { get; set; }

        [Required(ErrorMessage = "Items list is required")]
        [MinLength(1, ErrorMessage = "Items list cannot be empty")]
        public List<InventoryItemDTO> Items { get; set; } = new List<InventoryItemDTO>();
    }
}
