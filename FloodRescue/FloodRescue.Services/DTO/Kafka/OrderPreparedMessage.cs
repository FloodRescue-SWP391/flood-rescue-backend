using System;
using System.Collections.Generic;

namespace FloodRescue.Services.DTO.Kafka
{
    public class OrderPreparedMessage
    {
        public Guid ReliefOrderID { get; set; }
        public Guid RescueRequestID { get; set; }
        public Guid? RescueTeamID { get; set; }
        public Guid? ManagerID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PreparedTime { get; set; }
        public List<OrderPreparedItemMessage> Items { get; set; } = new();
    }

    public class OrderPreparedItemMessage
    {
        public int ReliefItemID { get; set; }
        public int Quantity { get; set; }
    }
}
