using System;
using System.Text.Json;
using System.Threading.Tasks;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.SignalR; // Corrected namespace reference
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.SharedSetting;

namespace FloodRescue.Services.Implements.Kafka.Handlers
{
    public class ReliefOrderHandler : IKafkaHandler
    {
        private readonly IRealtimeNotificationService _realtimeNotificationService;

        public ReliefOrderHandler(IRealtimeNotificationService realtimeNotificationService)
        {
            _realtimeNotificationService = realtimeNotificationService;
        }

        public string Topic => KafkaSettings.RELIEF_ORDER_CREATED_TOPIC;

        public async Task HandleAsync(string message)
        {
            var kafkaMessage = JsonSerializer.Deserialize<ReliefOrderMessage>(message);
            if (kafkaMessage == null)
            {
                throw new InvalidOperationException("Invalid ReliefOrderMessage");
            }

            var notification = new ReliefOrderNotification
            {
                ReliefOrderId = kafkaMessage.ReliefOrderId,
                RescueRequestId = kafkaMessage.RescueRequestId,
                RescueTeamId = kafkaMessage.RescueTeamId,
                Status = kafkaMessage.Status,
                CreatedTime = kafkaMessage.CreatedTime,
                Message = $"Relief order created for rescue request {kafkaMessage.RescueRequestId}"
            };

            await _realtimeNotificationService.SendToAllAsync(
                method: "ReliefOrderCreated",
                message: notification);
        }
    }
}
