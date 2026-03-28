using System;

namespace FloodRescue.Services.DTO.Response.ReliefOrderResponse
{
    public class ReliefOrderListResponseDTO
    {
        public Guid ReliefOrderID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public DateTime? PreparedTime { get; set; }
        public DateTime? PickedUpTime { get; set; }
        public Guid? AssignedTeamID { get; set; }
        public string? TeamName { get; set; }
        public int TotalItems { get; set; }
        public bool CanPrepare { get; set; }
    }
}
