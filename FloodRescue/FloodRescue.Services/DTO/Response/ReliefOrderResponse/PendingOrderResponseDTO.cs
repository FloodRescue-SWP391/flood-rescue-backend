using System;

namespace FloodRescue.Services.DTO.Response.ReliefOrderResponse
{
    public class PendingOrderResponseDTO
    {
        public Guid ReliefOrderID { get; set; }
        public Guid RescueRequestID { get; set; }
        public DateTime CreatedTime { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public Guid? MissionID { get; set; }
        public string? MissionStatus { get; set; }
        public string? TeamName { get; set; }
    }
}
