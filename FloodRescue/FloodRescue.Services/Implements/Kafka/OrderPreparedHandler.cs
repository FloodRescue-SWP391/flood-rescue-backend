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
    public class OrderPreparedHandler : IKafkaHandler
    {
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderPreparedHandler> _logger;

        public OrderPreparedHandler(IRealtimeNotificationService realtimeNotificationService, IMapper mapper, ILogger<OrderPreparedHandler> logger)
        {
            _realtimeNotificationService = realtimeNotificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public string Topic => KafkaSettings.ORDER_PREPARED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[OrderPreparedHandler - Kafka Consumer] Received message on topic {topic}", Topic);

            try
            {
                var kafkaMessage = JsonSerializer.Deserialize<OrderPreparedMessage>(message);

                if (kafkaMessage == null || kafkaMessage.ReliefOrderID == Guid.Empty)
                {
                    _logger.LogWarning("[OrderPreparedHandler - Kafka Consumer] Invalid message received on Topic {Topic}. Skipping message.", Topic);
                    return;
                }

                _logger.LogInformation("[OrderPreparedHandler - Kafka Consumer] Start to mapping from kafka message to notification");

                OrderPreparedNotification notification = _mapper.Map<OrderPreparedNotification>(kafkaMessage);
                notification.Message = $"Relief Order {kafkaMessage.ReliefOrderID} has been prepared and is ready for pickup.";

                _logger.LogInformation("[OrderPreparedHandler - SignalR] Start to send notification to Rescue Team ID: {ID}", kafkaMessage.RescueTeamID);

                // Send notification to Rescue Team Leader
                await _realtimeNotificationService.SendToGroupAsync(
                    groupName: GroupSettings.RESCUE_TEAM_GROUP,
                    method: "OrderPrepared",
                    message: notification);

                _logger.LogInformation("[OrderPreparedHandler - SignalR] Send notification successfully to Rescue Team ID: {ID}", kafkaMessage.RescueTeamID);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[OrderPreparedHandler - Error] Failed to deserialize JSON from topic {Topic}. Message skipped.", Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrderPreparedHandler - Error] Unexpected error processing message on topic {Topic}. Consumer will retry.", Topic);
                throw;
            }
        }
    }
}
