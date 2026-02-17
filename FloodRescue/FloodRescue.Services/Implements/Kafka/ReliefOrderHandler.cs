using System;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Core;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.SignalR; // Corrected namespace reference
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FloodRescue.Services.Implements.Kafka
{
    public class ReliefOrderHandler : IKafkaHandler
    {
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<ReliefOrderHandler> _logger;
        public ReliefOrderHandler(IRealtimeNotificationService realtimeNotificationService, IMapper mapper, ILogger<ReliefOrderHandler> logger)
        {
            _realtimeNotificationService = realtimeNotificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public string Topic => KafkaSettings.RELIEF_ORDER_CREATED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[ReliefOrderHandler- Kafka Consumer] Received message on topic {topic}", Topic);

            try
            {
                var kafkaMessage = JsonSerializer.Deserialize<ReliefOrderMessage>(message);

                if (kafkaMessage == null || kafkaMessage.ReliefOrderID == Guid.Empty)
                {
                    _logger.LogWarning("[ReliefOrderHandler - Kafka Consumer] Invalid message received on Topic {Topic}. Skipping message.", Topic);
                    return;
                }

                //Soạn notification phải có description rồi mới gửi cho manager, còn rescue team thì không cần
                //Trong auto mapper không cần description

                _logger.LogInformation("[ReliefOrderHandler - Kafka Consumer] Start to mapping from kafka message to notification");

                ReliefOrderNotification managerNotification = _mapper.Map<ReliefOrderNotification>(kafkaMessage);

                //description của citizen gửi trong rescue request để manager biết là cần chuẩn bị cái gì
                managerNotification.Message = kafkaMessage.Description;

                _logger.LogInformation("[ReliefOrderHandler - SignalR] Start to send information to Manager and Rescue Team ID: {ID}", kafkaMessage.RescueTeamID.ToString());

                //Signal R phải gửi cho cả manager với lại rescue team
                await _realtimeNotificationService.SendToGroupAsync(
                    groupName: GroupSettings.MANAGER_GROUP,
                    method: "ReliefOrderCreatedCoordinator",
                    message: managerNotification);  

                _logger.LogInformation("[ReliefOrderHandler - SignalR] Send notification sucessfully to Manager and Rescue Team ID: {ID} ", kafkaMessage.RescueTeamID.ToString());
            }
            catch(JsonException ex)
            {
                _logger.LogError(ex, "[ReliefOrderHandler - Error] Failed to deserialize JSON from topic {Topic}. Message skipped.", Topic);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[ReliefOrderHandler - Error] Unexpected error processing message on topic {Topic}. Consumer will retry.", Topic);
                throw;
            }


        }
    }
}
