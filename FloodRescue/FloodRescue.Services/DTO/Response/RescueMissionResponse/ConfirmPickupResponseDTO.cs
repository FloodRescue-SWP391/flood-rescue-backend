using System;

namespace FloodRescue.Services.DTO.Response.RescueMissionResponse
{
    public class ConfirmPickupResponseDTO
    {
        public Guid ReliefOrderID { get; set; }
        public Guid RescueMissionID { get; set; }
        public Guid RescueRequestID { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime? PickedUpTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
