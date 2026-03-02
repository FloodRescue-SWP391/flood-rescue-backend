using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.InventoryResponse
{
    /// <summary>
    /// DTO hiển thị thông tin tồn kho của từng item
    /// </summary>
    public class InventoryItemResponseDTO
    {
        public Guid InventoryID { get; set; }
        public int ReliefItemID { get; set; }
        public string ReliefItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
