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
    public class IncidentResolvedHandler : IKafkaHandler
    {
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<IncidentResolvedHandler> _logger;

        public IncidentResolvedHandler(IRealtimeNotificationService realtimeNotificationService, IMapper mapper, ILogger<IncidentResolvedHandler> logger)
        {
            _realtimeNotificationService = realtimeNotificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public string Topic => KafkaSettings.INCIDENT_RESOLVED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[IncidentResolvedHandler - Kafka Consumer] Received message on topic {topic}", Topic);

            try
            {
                var kafkaMessage = JsonSerializer.Deserialize<IncidentResolvedMessage>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (kafkaMessage == null || kafkaMessage.IncidentReportID == Guid.Empty)
                {
                    _logger.LogWarning("[IncidentResolvedHandler - Kafka Consumer] Invalid message received on Topic {Topic}. Skipping message.", Topic);
                    return;
                }

                _logger.LogInformation("[IncidentResolvedHandler - Kafka Consumer] Processing resolved incident for IncidentID: {IncidentID}, MissionID: {MissionID}", kafkaMessage.IncidentReportID, kafkaMessage.RescueMissionID);

                IncidentResolvedNotification notification = _mapper.Map<IncidentResolvedNotification>(kafkaMessage);
                notification.Message = $"Mission {kafkaMessage.RescueMissionID} has been cancelled due to incident resolution. Team {kafkaMessage.TeamName} is now available and released from duty.";

                // Bắn SignalR tới Rescue Team Leader của đội liên quan (báo cho leader biết nhiệm vụ đã hủy, đội đã được giải phóng)
                var leaderGroupName = $"Team_{kafkaMessage.RescueTeamID}_Leader";

                await _realtimeNotificationService.SendToGroupAsync(
                    groupName: leaderGroupName,
                    method: "IncidentResolved",
                    message: notification);

                _logger.LogInformation("[IncidentResolvedHandler - SignalR] Sent IncidentResolvedNotification to group {GroupName} for IncidentID {IncidentID}", leaderGroupName, kafkaMessage.IncidentReportID);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[IncidentResolvedHandler - Error] Failed to deserialize JSON from topic {Topic}. Message skipped.", Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentResolvedHandler - Error] Unexpected error processing message on topic {Topic}. Consumer will retry.", Topic);
                throw;
            }
        }
    }
}
