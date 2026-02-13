using AutoMapper;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.SignalR;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements.Kafka
{
    public class TeamAcceptedHandler : IKafkaHandler
    {
        private readonly ILogger<TeamAcceptedHandler> _logger;
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IMapper _mapper;

        public TeamAcceptedHandler(ILogger<TeamAcceptedHandler> logger, IRealtimeNotificationService realtimeNotificationService, IMapper mapper)
        {
            _logger = logger;
            _realtimeNotificationService = realtimeNotificationService;
            _mapper = mapper;
        }

        public string Topic => KafkaSettings.TEAM_ACCEPTED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[TeamAcceptedHandler - Kafka Consumer] Received message on topic {topic}", Topic);

            try
            {
                TeamAcceptedMessage? acceptedMessage = JsonSerializer.Deserialize<TeamAcceptedMessage>(message , new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                //mapper TeamAcceptedMessage -> TeamAcceptedNotification

                if (acceptedMessage == null || acceptedMessage.RescueMissionID == Guid.Empty)
                {
                    _logger.LogWarning("[TeamAcceptedHandler - Kafka Consumer] Invalid message received on Topic {Topic}. Skipping message.", Topic);
                    return;
                }

                TeamAcceptedNotification notification = _mapper.Map<TeamAcceptedNotification>(acceptedMessage);

                notification.Title = "Rescue Team Accepted Your Request";
                notification.NotificationType = "TeamAccepted";
                notification.Message = $"Your rescue request {acceptedMessage.RequestShortCode} has been accepted by team {acceptedMessage.TeamName}.";

                if (acceptedMessage.CoordinatorID.HasValue)
                {
                    await _realtimeNotificationService.SendToUserAsync(userId: acceptedMessage.CoordinatorID.Value.ToString(), method: "ReceiveTeamResponse", message: notification);

                    _logger.LogInformation("[TeamAcceptedHandler - SignalR] Sent TeamAcceptedNotification to Coordinator {CoordinatorID} for RescueRequest {RescueRequestID}", acceptedMessage.CoordinatorID, acceptedMessage.RescueRequestID);
                }


                await _realtimeNotificationService.SendToGroupAsync(groupName: GroupSettings.RESCUE_COORDINATOR_GROUP, method: "ReceiveTeamResponse", message: notification);

                _logger.LogInformation("[TeamAcceptedHandler - SignalR] Sent TeamAcceptedNotification to Coordinator group for RescueRequest {RescueRequestID}", acceptedMessage.RescueRequestID);  


            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[TeamAcceptedHandler - Error] Failed to deserialize JSON from topic {Topic}. Message skipped.", Topic);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[TeamAcceptedHandler - Error] Unexpected error processing message on topic {Topic}. Consumer will retry.", Topic);
                throw;

            }
        }
    }
}
