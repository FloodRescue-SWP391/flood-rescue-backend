using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using FloodRescue.Services.DTO.SignalR;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.Interface.RescueMission;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using RescueMissionEntity = FloodRescue.Repositories.Entites.RescueMission;
using RescueRequestEntity = FloodRescue.Repositories.Entites.RescueRequest;
using RescueTeamEntity = FloodRescue.Repositories.Entites.RescueTeam;
using RescueTeamMemberEntity = FloodRescue.Repositories.Entites.RescueTeamMember;

namespace FloodRescue.Services.Implements.RescueMission
{
    public class RescueMissionService : IRescueMissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RescueMissionService> _logger;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly IRealtimeNotificationService _realtimeNotificationService;

        public RescueMissionService(IUnitOfWork unitOfWork,
            ILogger<RescueMissionService> logger,
            IKafkaProducerService kafkaProducer,
            IRealtimeNotificationService realtimeNotificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _realtimeNotificationService = realtimeNotificationService;
        }

        public async Task<DispatchMissionResponseDTO?> DispatchMissionAsync(DispatchMissionRequestDTO request)
        {
            _logger.LogInformation("Starting Dispatch with Request ID: {RequestID}, Team ID: {TeamID}", request.RescueRequestID, request.RescueTeamID);

            //Sửa lại pending
            // Tìm ra Rescue Request chính xác và đang ở trạng thái "Pending"
            RescueRequestEntity? rescueRequest = await _unitOfWork.RescueRequests.GetAsync((RescueRequestEntity rr) => rr.RescueRequestID == request.RescueRequestID && rr.Status == "Pending" && !rr.IsDeleted);

            if (rescueRequest == null)
            {
                _logger.LogWarning("Rescue Request with ID: {RequestID} not found or not in Pending status", request.RescueRequestID);
                return null;
            }

            //Coi rescue team id có tồn tại và đang ở trạng thái "Available" 
            RescueTeamEntity? rescueTeam = await _unitOfWork.RescueTeams.GetAsync((RescueTeamEntity rt) => rt.RescueTeamID == request.RescueTeamID && rt.CurrentStatus == "Available" && !rt.IsDeleted);

            if (rescueTeam == null)
            {
                _logger.LogWarning("Rescue Team with ID: {TeamID} not found or not Available", request.RescueTeamID);
                return null;
            }

            //Cập nhật trạng thái của Rescue Request thành "Processing"
            rescueRequest.Status = "Processing";

            //Tạo mới Rescue Mission
            RescueMissionEntity newMission = new RescueMissionEntity
            {
                RescueMissionID = Guid.NewGuid(),
                RescueRequestID = request.RescueRequestID,
                RescueTeamID = request.RescueTeamID,
                Status = "Assigned",
                AssignedAt = DateTime.UtcNow,
                StartTime = null,
                EndTime = null,
                IsDeleted = false

            };

            await _unitOfWork.RescueMissions.AddAsync(newMission);

            //Cập nhật trạng thái của Rescue Team thành "Busy"
            rescueTeam.CurrentStatus = "Busy";


            int saveResult = await _unitOfWork.SaveChangesAsync();

            if (saveResult <= 0)
            {
                _logger.LogError("Failed to dispatch mission for Request ID: {RequestID} and Team ID: {TeamID} - Could not Save Change", request.RescueRequestID, request.RescueTeamID);
                return null;
            }

            //gửi mesage qua kafka
            MissionAssignedMessage kafkaMessage = new MissionAssignedMessage
            {
                MissionID = newMission.RescueMissionID,
                AssignedAt = newMission.AssignedAt,
                RescueRequestID = rescueRequest.RescueRequestID,
                RequestShortCode = rescueRequest.ShortCode,
                CitizenPhone = rescueRequest.CitizenPhone,
                CitizenName = rescueRequest.CitizenName,
                Address = rescueRequest.Address,
                LocationLatitude = rescueRequest.LocationLatitude,
                LocationLongitude = rescueRequest.LocationLongitude,
                PeopleCount = rescueRequest.PeopleCount,
                RescueTeamID = rescueTeam.RescueTeamID,
                TeamName = rescueTeam.TeamName

            };

            await _kafkaProducer.ProduceAsync(topic: KafkaSettings.MISSION_ASSIGN_TOPIC, key: newMission.RescueMissionID.ToString(), message: kafkaMessage);

            _logger.LogInformation("Kafka message sent to topic {Topic}", KafkaSettings.MISSION_ASSIGN_TOPIC);

            MissionAssignedNotification signalRNotification = new MissionAssignedNotification
            {
                Title = "New Rescue Mission Assigned",
                NotificationType = "MissionAssigned", //1 cái label để front end dựa vào đây thao tác khi nhận thông báo từ SignalR
                MissionID = newMission.RescueMissionID,
                MissionStatus = newMission.Status,
                AssignedAt = newMission.AssignedAt,
                RequestShortCode = rescueRequest.ShortCode,
                CitizenName = rescueRequest.CitizenName,
                CitizenPhone = rescueRequest.CitizenPhone,
                Address = rescueRequest.Address,
                LocationLatitude = rescueRequest.LocationLatitude,
                LocationLongitude = rescueRequest.LocationLongitude,
                PeopleCount = rescueRequest.PeopleCount,
                Description = rescueRequest.Description,
                ActionMessage = "Please proceed to the rescue location immediately in 5 minutes."

            };

            //Gửi notification qua SignalR
            //xíu hồi tạo connection on riêng biệt bên fe để lắng nghe
            //còn hàm invoke để gửi xin gia nhập team id trong signalR hub là 1 hàm nữa - tổng cộng là 2 hàm là đủ
            await _realtimeNotificationService.SendToGroupAsync(groupName: rescueTeam.RescueTeamID.ToString(), method: "ReceiveMissionNotification", message: signalRNotification);


            _logger.LogInformation("SignalR notification sent to group {GroupName}", rescueTeam.RescueTeamID.ToString());

            _logger.LogInformation("Successfully dispatched mission with ID: {MissionID} for Request ID: {RequestID} and Team ID: {TeamID}", newMission.RescueMissionID, request.RescueRequestID, request.RescueTeamID);

            return new DispatchMissionResponseDTO
            {
                RescueMissionID = newMission.RescueMissionID,
                RescueRequestID = rescueRequest.RescueRequestID,
                RequestShortCode = rescueRequest.ShortCode,
                RescueTeamID = rescueTeam.RescueTeamID,
                TeamName = rescueTeam.TeamName,
                MissionStatus = newMission.Status,
                AssignedAt = newMission.AssignedAt,
                Message = $"Rescue mission dispatched successfully for Team {rescueTeam.RescueTeamID} - Team Name {rescueTeam.TeamName}."
            };
        }
    }
}
