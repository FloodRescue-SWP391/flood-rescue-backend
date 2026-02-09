using System.Runtime.CompilerServices;
using System.Text.Json;
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
  
    public DispatchMissionKafkaHandler(IRealtimeNotificationService realtimeNotificationService, ILogger<DispatchMissionKafkaHandler> logger)
    {
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }
   
    public string Topic => KafkaSettings.MISSION_ASSIGN_TOPIC;

    public async Task HandleAsync(string message)
    {
        _logger.LogInformation("Received message on topic {topic}", Topic);

        try
        {
            MissionAssignedMessage? missionAssigned = JsonSerializer.Deserialize<MissionAssignedMessage>(message);

            if(missionAssigned == null || missionAssigned.MissionID == Guid.Empty || missionAssigned.RescueTeamID == Guid.Empty)
            {
                _logger.LogWarning("Data invalid (Null or Empty IDs). Skipping processing.");
                return;   
            }

            MissionAssignedNotification notification = new MissionAssignedNotification
            {
                    Title = "New Rescue Mission Assigned",
                    NotificationType = "MissionAssigned", //1 cái label để front end dựa vào đây thao tác khi nhận thông báo từ SignalR
                    MissionID = missionAssigned.MissionID,
                    MissionStatus = missionAssigned.MissionStatus,
                    AssignedAt = missionAssigned.AssignedAt,
                    RequestShortCode = missionAssigned.RequestShortCode,
                    CitizenName = missionAssigned.CitizenName,
                    CitizenPhone = missionAssigned.CitizenPhone,
                    Address = missionAssigned.Address,
                    LocationLatitude = missionAssigned.LocationLatitude,
                    LocationLongitude = missionAssigned.LocationLongitude,
                    PeopleCount = missionAssigned.PeopleCount,
                    Description = missionAssigned.Description,
                    ActionMessage = "Please proceed to the rescue location immediately in 5 minutes."
            };

            //Gửi notification qua SignalR
            //xíu hồi tạo connection on riêng biệt bên fe để lắng nghe
            //còn hàm invoke để gửi xin gia nhập team id trong signalR hub là 1 hàm nữa - tổng cộng là 2 hàm là đủ
            await _realtimeNotificationService.SendToGroupAsync(groupName: missionAssigned.RescueTeamID.ToString(), method: "ReceiveMissionNotification", message: notification);

            _logger.LogInformation("SignalR notification sent to group {GroupName}", missionAssigned.RescueTeamID.ToString());

        }catch(JsonException ex)
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