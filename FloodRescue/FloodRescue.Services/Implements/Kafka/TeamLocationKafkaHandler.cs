using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.Implements.RealTimeNoti;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FloodRescue.Services.Implements.Kafka
{
    public class TeamLocationKafkaHandler : IKafkaHandler
    {
        private readonly ICacheService _cacheService;
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly ILogger<TeamLocationKafkaHandler> _logger;


        private const string TRACKING_CACHE_PREFIX = "Tracking:TeamLocation:";


        public string Topic => KafkaSettings.TEAM_LOCATION_TRACKING_TOPIC;

        public TeamLocationKafkaHandler(
          ICacheService cacheService,
          IRealtimeNotificationService realtimeNotificationService,
          ILogger<TeamLocationKafkaHandler> logger)
        {
            _cacheService = cacheService;
            _realtimeNotificationService = realtimeNotificationService;
            _logger = logger;
        }

        public async Task HandleAsync(string message)
        {
            try
            {
                TeamLocationKafkaMessage? locationMessage = JsonSerializer.Deserialize<TeamLocationKafkaMessage>(
                   message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


                if (locationMessage == null || locationMessage.TeamID == Guid.Empty)
                {
                    _logger.LogWarning("[TeamLocationKafkaHandler] Invalid location message. Skipping.");
                    return;
                }

                string cacheKey = $"{TRACKING_CACHE_PREFIX}{locationMessage.TeamID}";

                await _cacheService.SetAsync(cacheKey, locationMessage, TimeSpan.FromMinutes(10));


                _logger.LogInformation("[TeamLocationKafkaHandler - Redis] Cached location for TeamID: {TeamID}. Lat: {Lat}, Lng: {Lng}",
                    locationMessage.TeamID, locationMessage.Latitude, locationMessage.Longitude);

                // 2. Broadcast qua SignalR → Coordinator UI cập nhật bản đồ
                // Method "TeamLocationUpdated" — FE listen .on("TeamLocationUpdated", ...)
                await _realtimeNotificationService.SendToGroupAsync(
                    groupName: GroupSettings.RESCUE_COORDINATOR_GROUP,
                    method: "TeamLocationUpdated",
                    message: new
                    {
                        locationMessage.TeamID,
                        locationMessage.RescueMissionID,
                        locationMessage.Latitude,
                        locationMessage.Longitude,
                        locationMessage.ClientTimestamp
                    });

                _logger.LogInformation("[TeamLocationKafkaHandler - SignalR] Broadcast location to Coordinator group for TeamID: {TeamID}",
                    locationMessage.TeamID);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[TeamLocationKafkaHandler - Error] Failed to deserialize location message. Skipping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TeamLocationKafkaHandler - Error] Unexpected error processing location. Consumer will retry.");
                throw;
            }
        }
    }
}

