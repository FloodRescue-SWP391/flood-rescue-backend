using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.InventoryRequest
{
    public class AdjustInventoryItemDTO
    {
        [Required(ErrorMessage = "ReliefItemID is required")]
        public int ReliefItemID { get; set; }

        [Required(ErrorMessage = "AdjustmentQuantity is required")]
        public int AdjustmentQuantity { get; set; }
    }

    public class AdjustInventoryRequestDTO
    {
        [Required(ErrorMessage = "WarehouseID is required")]
        public int WarehouseID { get; set; }

        [Required(ErrorMessage = "Items list is required")]
        [MinLength(1, ErrorMessage = "Items list must contain at least one item")]
        public List<AdjustInventoryItemDTO> Items { get; set; } = new();
    }
}
