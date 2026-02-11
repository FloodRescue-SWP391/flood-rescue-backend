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
    public class TeamRejectedHandler : IKafkaHandler
    {
        private readonly ILogger<TeamRejectedHandler> _logger;
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IMapper _mapper;

        public TeamRejectedHandler(ILogger<TeamRejectedHandler> logger, IRealtimeNotificationService realtimeNotificationService, IMapper mapper)
        {
            _logger = logger;
            _realtimeNotificationService = realtimeNotificationService;
            _mapper = mapper;
        }

        public string Topic => KafkaSettings.TEAM_REJECTED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("Received message on topic {topic}", Topic);

            try
            {

                TeamRejectedMessage? rejectedMessage = JsonSerializer.Deserialize<TeamRejectedMessage>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });  

                if (rejectedMessage == null || rejectedMessage.RescueMissionID == Guid.Empty)
                {
                    _logger.LogWarning("Invalid message format or missing RescueMissionID on Topic {Topic}. Skipping message.", Topic);
                    return;
                }

                TeamRejectedNotification notification = _mapper.Map<TeamRejectedNotification>(rejectedMessage); 

                notification.Title = "Rescue Team Rejected";    
                notification.NotificationType = "TeamRejected";
                notification.ActionMessage = $"The rescue team '{notification.TeamName}' has rejected the mission for request '{notification.RequestShortCode}'.";

                if (rejectedMessage.CoordinatorID.HasValue)
                {
                    await _realtimeNotificationService.SendToUserAsync(userId: rejectedMessage.CoordinatorID.Value.ToString(), method: "ReceiveTeamResponse", message: notification);
                }


                await _realtimeNotificationService.SendToGroupAsync(groupName: "Coordinator", method: "ReceiveTeamResponse", message: notification);

                _logger.LogInformation("Processed TeamRejected message for RescueMissionID {RescueMissionID} with Reason {Reason}", rejectedMessage.RescueMissionID, notification.RejectReason);

            }
            catch(JsonException ex)
            {
                _logger.LogError(ex, "JSON Format Error on Topic {Topic}. Skipping message.", Topic);   
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "System Error processing message on Topic {Topic}. Consumer will retry...", Topic);    
                throw;
            }
        }
    }
}
