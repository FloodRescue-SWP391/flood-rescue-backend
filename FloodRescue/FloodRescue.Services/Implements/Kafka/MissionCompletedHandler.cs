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
    public class MissionCompletedHandler : IKafkaHandler
    {
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<MissionCompletedHandler> _logger;

        public MissionCompletedHandler(IRealtimeNotificationService realtimeNotificationService, IMapper mapper, ILogger<MissionCompletedHandler> logger)
        {
            _realtimeNotificationService = realtimeNotificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public string Topic => KafkaSettings.MISSION_COMPLETED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[MissionCompletedHandler - Kafka Consumer] Received message on topic {topic}", Topic);

            try
            {
                var kafkaMessage = JsonSerializer.Deserialize<MissionCompletedMessage>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (kafkaMessage == null || kafkaMessage.RescueMissionID == Guid.Empty)
                {
                    _logger.LogWarning("[MissionCompletedHandler - Kafka Consumer] Invalid message received on Topic {Topic}. Skipping message.", Topic);
                    return;
                }

                _logger.LogInformation("[MissionCompletedHandler - Kafka Consumer] Start to mapping from kafka message to notification for MissionID: {MissionID}", kafkaMessage.RescueMissionID);

                MissionCompletedNotification notification = _mapper.Map<MissionCompletedNotification>(kafkaMessage);
                notification.Message = $"Rescue mission {kafkaMessage.RescueMissionID} has been completed by Team {kafkaMessage.TeamName}. Request {kafkaMessage.RequestShortCode} status: {kafkaMessage.RequestStatus}.";

                // Gửi notification tới Coordinator cụ thể đã đăng kí nhiệm vụ này
                if (kafkaMessage.CoordinatorID.HasValue)
                {
                    await _realtimeNotificationService.SendToUserAsync(
                        userId: kafkaMessage.CoordinatorID.Value.ToString(),
                        method: "MissionCompleted",
                        message: notification);

                    _logger.LogInformation("[MissionCompletedHandler - SignalR] Sent MissionCompletedNotification to Coordinator {CoordinatorID} for MissionID {MissionID}", kafkaMessage.CoordinatorID, kafkaMessage.RescueMissionID);
                }

                // Gửi notification tới group Coordinator
                await _realtimeNotificationService.SendToGroupAsync(
                    groupName: GroupSettings.RESCUE_COORDINATOR_GROUP,
                    method: "MissionCompleted",
                    message: notification);

                _logger.LogInformation("[MissionCompletedHandler - SignalR] Sent MissionCompletedNotification to Coordinator group for MissionID {MissionID}", kafkaMessage.RescueMissionID);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[MissionCompletedHandler - Error] Failed to deserialize JSON from topic {Topic}. Message skipped.", Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MissionCompletedHandler - Error] Unexpected error processing message on topic {Topic}. Consumer will retry.", Topic);
                throw;
            }
        }
    }
}
