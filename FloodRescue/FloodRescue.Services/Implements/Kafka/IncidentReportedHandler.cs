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
    public class IncidentReportedHandler : IKafkaHandler
    {
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<IncidentReportedHandler> _logger;

        public IncidentReportedHandler(IRealtimeNotificationService realtimeNotificationService, IMapper mapper, ILogger<IncidentReportedHandler> logger)
        {
            _realtimeNotificationService = realtimeNotificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public string Topic => KafkaSettings.INCIDENT_ALERT_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[IncidentReportedHandler - Kafka Consumer] Received message on topic {topic}", Topic);

            try
            {
                var kafkaMessage = JsonSerializer.Deserialize<IncidentReportedMessage>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (kafkaMessage == null || kafkaMessage.IncidentReportID == Guid.Empty)
                {
                    _logger.LogWarning("[IncidentReportedHandler - Kafka Consumer] Invalid message received on Topic {Topic}. Skipping message.", Topic);
                    return;
                }

                _logger.LogInformation("[IncidentReportedHandler - Kafka Consumer] Processing incident report for IncidentID: {IncidentID}, MissionID: {MissionID}", kafkaMessage.IncidentReportID, kafkaMessage.RescueMissionID);

                IncidentReportedNotification notification = _mapper.Map<IncidentReportedNotification>(kafkaMessage);
                notification.Message = $"URGENT: Team {kafkaMessage.TeamName} reported an incident during mission {kafkaMessage.RescueMissionID}. Title: {kafkaMessage.Title}. Mission has been locked.";

                // Bắn SignalR khẩn cấp đến tất cả Coordinator
                await _realtimeNotificationService.SendToGroupAsync(
                    groupName: GroupSettings.RESCUE_COORDINATOR_GROUP,
                    method: "IncidentReported",
                    message: notification);

                _logger.LogInformation("[IncidentReportedHandler - SignalR] Sent IncidentReportedNotification to Coordinator group for IncidentID {IncidentID}", kafkaMessage.IncidentReportID);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[IncidentReportedHandler - Error] Failed to deserialize JSON from topic {Topic}. Message skipped.", Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentReportedHandler - Error] Unexpected error processing message on topic {Topic}. Consumer will retry.", Topic);
                throw;
            }
        }
    }
}