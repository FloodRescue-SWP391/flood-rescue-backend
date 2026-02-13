using System.Runtime.CompilerServices;
using System.Text.Json;
using AutoMapper;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.SignalR;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.Interface.RescueMission;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
public class DispatchMissionKafkaHandler : IKafkaHandler
{
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ILogger<DispatchMissionKafkaHandler> _logger;
    private readonly IMapper _mapper;
  
    public DispatchMissionKafkaHandler(IRealtimeNotificationService realtimeNotificationService, ILogger<DispatchMissionKafkaHandler> logger, IMapper mapper)
    {
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
        _mapper = mapper;
    }
   
    public string Topic => KafkaSettings.MISSION_ASSIGN_TOPIC;

    public async Task HandleAsync(string message)
    {
        _logger.LogInformation("[DispatchMissionKafkaHandler - Kafka Consumer] Received message on topic {topic}", Topic);

        try
        {
            MissionAssignedMessage? missionAssigned = JsonSerializer.Deserialize<MissionAssignedMessage>(message);

            if(missionAssigned == null || missionAssigned.MissionID == Guid.Empty || missionAssigned.RescueTeamID == Guid.Empty)
            {
                _logger.LogWarning("[DispatchMissionKafkaHandler - Kafka Consumer] Data invalid (Null or Empty IDs). Skipping processing.");
                return;   
            }

            //xài auto mapper
            MissionAssignedNotification notification = _mapper.Map<MissionAssignedNotification>(missionAssigned);         

            //Gửi notification qua SignalR
            //xíu hồi tạo connection on riêng biệt bên fe để lắng nghe
            //còn hàm invoke để gửi xin gia nhập team id trong signalR hub là 1 hàm nữa - tổng cộng là 2 hàm là đủ
            await _realtimeNotificationService.SendToGroupAsync(groupName: missionAssigned.RescueTeamID.ToString(), method: "ReceiveMissionNotification", message: notification);

            _logger.LogInformation("[DispatchMissionKafkaHandler - SignalR] SignalR notification sent to group {GroupName}", missionAssigned.RescueTeamID.ToString());

        }catch(JsonException ex)
        {
            _logger.LogError(ex, "[DispatchMissionKafkaHandler - Error] Failed to deserialize JSON from topic {Topic}. Message skipped.", Topic);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "[DispatchMissionKafkaHandler - Error] Unexpected error processing message on topic {Topic}. Consumer will retry.", Topic);
            throw;
        }
      
    }
} 