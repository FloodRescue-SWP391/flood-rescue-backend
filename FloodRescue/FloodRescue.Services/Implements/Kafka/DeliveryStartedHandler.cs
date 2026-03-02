using System;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.SignalR;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;

namespace FloodRescue.Services.Implements.Kafka
{
    public class DeliveryStartedHandler : IKafkaHandler
    {
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<DeliveryStartedHandler> _logger;

        public DeliveryStartedHandler(IRealtimeNotificationService realtimeNotificationService, IMapper mapper, ILogger<DeliveryStartedHandler> logger)
        {
            _realtimeNotificationService = realtimeNotificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public string Topic => KafkaSettings.DELIVERY_STARTED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[DeliveryStartedHandler - Kafka Consumer] Received message on topic {topic}", Topic);

            try
            {
                var kafkaMessage = JsonSerializer.Deserialize<DeliveryStartedMessage>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (kafkaMessage == null || kafkaMessage.ReliefOrderID == Guid.Empty)
                {
                    _logger.LogWarning("[DeliveryStartedHandler - Kafka Consumer] Invalid message received on Topic {Topic}. Skipping message.", Topic);
                    return;
                }

                _logger.LogInformation("[DeliveryStartedHandler - Kafka Consumer] Start to mapping from kafka message to notification");

                DeliveryStartedNotification notification = _mapper.Map<DeliveryStartedNotification>(kafkaMessage);
                notification.Message = $"Relief Order {kafkaMessage.ReliefOrderID} has been picked up by Team {kafkaMessage.TeamName}. Delivery is now in progress.";

                // Gửi notification tới Coordinator cụ thể
                if (kafkaMessage.CoordinatorID.HasValue)
                {
                    await _realtimeNotificationService.SendToUserAsync(
                        userId: kafkaMessage.CoordinatorID.Value.ToString(),
                        method: "DeliveryStarted",
                        message: notification);

                    _logger.LogInformation("[DeliveryStartedHandler - SignalR] Sent DeliveryStartedNotification to Coordinator {CoordinatorID} for ReliefOrder {OrderID}", kafkaMessage.CoordinatorID, kafkaMessage.ReliefOrderID);
                }

                // Gửi notification tới group Coordinator
                await _realtimeNotificationService.SendToGroupAsync(
                    groupName: GroupSettings.RESCUE_COORDINATOR_GROUP,
                    method: "DeliveryStarted",
                    message: notification);

                _logger.LogInformation("[DeliveryStartedHandler - SignalR] Sent DeliveryStartedNotification to Coordinator group for ReliefOrder {OrderID}", kafkaMessage.ReliefOrderID);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[DeliveryStartedHandler - Error] Failed to deserialize JSON from topic {Topic}. Message skipped.", Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeliveryStartedHandler - Error] Unexpected error processing message on topic {Topic}. Consumer will retry.", Topic);
                throw;
            }
        }
    }
}
