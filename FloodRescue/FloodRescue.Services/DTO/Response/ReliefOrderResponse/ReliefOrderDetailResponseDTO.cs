using System;
using System.Collections.Generic;

namespace FloodRescue.Services.DTO.Response.ReliefOrderResponse
{
    public class ReliefOrderItemDTO
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ReliefOrderDetailResponseDTO
    {
        public Guid ReliefOrderID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public DateTime? PreparedTime { get; set; }
        public DateTime? PickedUpTime { get; set; }
        public Guid? AssignedTeamID { get; set; }
        public string? TeamName { get; set; }
        public string? Description { get; set; }
        public List<ReliefOrderItemDTO> Items { get; set; } = new();
    }
}
