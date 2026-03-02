using System;
using System.Collections.Generic;

namespace FloodRescue.Services.DTO.Response.InventoryResponse
{
    public class AdjustInventoryItemResponseDTO
    {
        public int ReliefItemID { get; set; }
        public string ReliefItemName { get; set; } = string.Empty;
        public int OldQuantity { get; set; }
        public int AdjustmentQuantity { get; set; }
        public int NewQuantity { get; set; }
    }

    public class AdjustInventoryResponseDTO
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime AdjustedAt { get; set; }
        public List<AdjustInventoryItemResponseDTO> AdjustedItems { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
