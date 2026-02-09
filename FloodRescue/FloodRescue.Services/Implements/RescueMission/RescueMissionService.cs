using AutoMapper;
using Azure;
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
        private readonly IMapper _mapper;
        

        public RescueMissionService(IUnitOfWork unitOfWork,
            ILogger<RescueMissionService> logger,
            IKafkaProducerService kafkaProducer,
           IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _mapper = mapper;
            
        }

        public async Task<DispatchMissionResponseDTO?> DispatchMissionAsync(DispatchMissionRequestDTO request)
        {

              _logger.LogInformation("Starting Dispatch with Request ID: {RequestID}, Team ID: {TeamID}", request.RescueRequestID, request.RescueTeamID);

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
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
                MissionAssignedMessage kafkaMessage = _mapper.Map<MissionAssignedMessage>(rescueRequest);

                _mapper.Map(rescueTeam, kafkaMessage);

                _mapper.Map(newMission, kafkaMessage);

                await _kafkaProducer.ProduceAsync(topic: KafkaSettings.MISSION_ASSIGN_TOPIC, key: newMission.RescueMissionID.ToString(), message: kafkaMessage);

                _logger.LogInformation("Kafka message sent to topic {Topic}", KafkaSettings.MISSION_ASSIGN_TOPIC);

                await transaction.CommitAsync();                

                _logger.LogInformation("Successfully dispatched mission with ID: {MissionID} for Request ID: {RequestID} and Team ID: {TeamID}", newMission.RescueMissionID, request.RescueRequestID, request.RescueTeamID);

                DispatchMissionResponseDTO response = _mapper.Map<DispatchMissionResponseDTO>(newMission);

                _mapper.Map(rescueRequest, response);

                _mapper.Map(rescueTeam, response);

                response.Message = $"Rescue mission dispatched successfully for Team {rescueTeam.RescueTeamID} - Team Name {rescueTeam.TeamName}.";

                return response;
                
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error dispatching mission. Transaction rolled back for Request ID: {RequestID}", request.RescueRequestID);

                throw;
            }
        }
    }
}
