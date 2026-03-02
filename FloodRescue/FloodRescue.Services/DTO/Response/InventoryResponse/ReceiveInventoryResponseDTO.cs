using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.InventoryResponse
{

    /// <summary>
    /// Chi tiết từng item đã được cập nhật
    /// </summary>
    public class InventoryItemResultDTO
    {
        public int ReliefItemID { get; set; }
        public string ReliefItemName { get; set; } = string.Empty;
        public int QuantityAdded { get; set; }
        public int NewTotalQuantity { get; set; }
        public bool IsNewRecord { get; set; }  // true nếu tạo mới, false nếu cộng dồn
    }

    /// <summary>
    /// Response sau khi nhập kho thành công
    /// </summary>

    public class ReceiveInventoryResponseDTO
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int TotalItemsProcessed { get; set; }
        public DateTime ProcessedAt { get; set; }
        public List<InventoryItemResultDTO> ItemResults { get; set; } = new List<InventoryItemResultDTO>();
        public string Message { get; set; } = string.Empty;
    }
}
