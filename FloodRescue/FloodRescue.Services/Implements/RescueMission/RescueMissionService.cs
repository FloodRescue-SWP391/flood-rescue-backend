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

            _logger.LogInformation("[RescueMissionService] Starting Dispatch with Request ID: {RequestID}, Team ID: {TeamID}", request.RescueRequestID, request.RescueTeamID);

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                //Sửa lại pending
                // Tìm ra Rescue Request chính xác và đang ở trạng thái "Pending"
                RescueRequestEntity? rescueRequest = await _unitOfWork.RescueRequests.GetAsync((RescueRequestEntity rr) => rr.RescueRequestID == request.RescueRequestID && rr.Status == RescueRequestSettings.PENDING_STATUS && !rr.IsDeleted);

                if (rescueRequest == null)
                {
                    _logger.LogWarning("[RescueMissionService - Sql Server] Rescue Request with ID: {RequestID} not found or not in Pending status", request.RescueRequestID);
                    return null;
                }

                //Coi rescue team id có tồn tại và đang ở trạng thái "Available" 
                RescueTeamEntity? rescueTeam = await _unitOfWork.RescueTeams.GetAsync((RescueTeamEntity rt) => rt.RescueTeamID == request.RescueTeamID && rt.CurrentStatus == RescueTeamSettings.AVAILABLE_STATUS && !rt.IsDeleted);

                if (rescueTeam == null)
                {
                    _logger.LogWarning("[RescueMissionService - Sql Server] Rescue Team with ID: {TeamID} not found or not Available", request.RescueTeamID);
                    return null;
                }

                //Cập nhật trạng thái của Rescue Request thành "Processing"
                rescueRequest.Status = RescueMissionSettings.PROCESSING_STATUS;

                //Tạo mới Rescue Mission
                RescueMissionEntity newMission = new RescueMissionEntity
                {
                    RescueMissionID = Guid.NewGuid(),
                    RescueRequestID = request.RescueRequestID,
                    RescueTeamID = request.RescueTeamID,
                    Status = RescueMissionSettings.ASSIGNED_STATUS,
                    AssignedAt = DateTime.UtcNow,
                    StartTime = null,
                    EndTime = null,
                    IsDeleted = false

                };

                await _unitOfWork.RescueMissions.AddAsync(newMission);

                //Cập nhật trạng thái của Rescue Team thành "Busy"
                rescueTeam.CurrentStatus = RescueTeamSettings.BUSY_STATUS;


                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogError("[RescueMissionService - Error] SaveChanges returned 0 rows during dispatch. RequestID: {RequestID}, TeamID: {TeamID}", request.RescueRequestID, request.RescueTeamID);
                    await transaction.RollbackAsync();  
                    return null;
                }

                //gửi mesage qua kafka
                MissionAssignedMessage kafkaMessage = _mapper.Map<MissionAssignedMessage>(rescueRequest);

                _mapper.Map(rescueTeam, kafkaMessage);

                _mapper.Map(newMission, kafkaMessage);

                await _kafkaProducer.ProduceAsync(topic: KafkaSettings.MISSION_ASSIGN_TOPIC, key: newMission.RescueMissionID.ToString(), message: kafkaMessage);

                _logger.LogInformation("[RescueMissionService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.MISSION_ASSIGN_TOPIC);               

                _logger.LogInformation("[RescueMissionService] Successfully dispatched mission with ID: {MissionID} for Request ID: {RequestID} and Team ID: {TeamID}", newMission.RescueMissionID, request.RescueRequestID, request.RescueTeamID);

                DispatchMissionResponseDTO response = _mapper.Map<DispatchMissionResponseDTO>(newMission);

                _mapper.Map(rescueRequest, response);

                _mapper.Map(rescueTeam, response);

                response.Message = $"Rescue mission dispatched successfully for Team {rescueTeam.RescueTeamID} - Team Name {rescueTeam.TeamName}.";

                await transaction.CommitAsync();

                return response;
                
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "[RescueMissionService - Error] Dispatch failed. Transaction rolled back. RequestID: {RequestID}", request.RescueRequestID);

                throw;
            }
        }

        public async Task<RespondMissionResponseDTO?> RespondMissionAsync(RespondMessageRequestDTO request)
        {
            _logger.LogInformation("[RescueMissionService] Starting Respond with MissionID: {MissionID}, IsAccepted: {IsAccepted}", request.RescueMissionID, request.IsAccepted);

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                //Tìm request đang pending
                RescueMissionEntity? rescueMission = await _unitOfWork.RescueMissions.GetAsync((RescueMissionEntity rm) => rm.RescueMissionID == request.RescueMissionID && rm.Status == RescueMissionSettings.ASSIGNED_STATUS && !rm.IsDeleted, rm => rm.RescueTeam!, rm => rm.RescueRequest!);

                if (rescueMission == null)
                {
                    _logger.LogWarning("[RescueMissionService - Sql Server] Rescue Mission with ID: {MissionID} not found or not in Assigned status", request.RescueMissionID);
                    return null;
                }

                RescueTeamEntity rescueTeam = rescueMission.RescueTeam!;    
                RescueRequestEntity rescueRequest = rescueMission.RescueRequest!;   

                DateTime respondedAt = DateTime.UtcNow;

                if (request.IsAccepted)
                {
                    rescueMission.Status = RescueMissionSettings.INPROGRESS_STATUS;    
                    rescueMission.StartTime = respondedAt;  
                    _logger.LogInformation("[RescueMissionService] Rescue Mission with ID: {MissionID} accepted and set to InProgress - Team {TeamName}", request.RescueMissionID, rescueTeam.TeamName);

                    // mappper rescue mission -> team accept message
                    // mapper rescue request -> team accept message 
                    // mapper rescue team -> team accept message    
                    TeamAcceptedMessage kafkaMessage = _mapper.Map<TeamAcceptedMessage>(rescueMission);
                    _mapper.Map(rescueRequest, kafkaMessage);
                    _mapper.Map(rescueTeam, kafkaMessage);

                    //gán field ngoài mapper
                    kafkaMessage.AcceptedAt = respondedAt;  

                    await _kafkaProducer.ProduceAsync(topic: KafkaSettings.TEAM_ACCEPTED_TOPIC, key: rescueMission.RescueMissionID.ToString(), message: kafkaMessage);
                    
                    _logger.LogInformation("[RescueMissionService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.TEAM_ACCEPTED_TOPIC);

                }
                else
                {
                    //mapper rescue mission -> team reject message  
                    //mapper rescue request -> team reject message  
                    //mapper rescue team -> team reject message
                  
                    rescueMission.Status = RescueMissionSettings.DECLINED_STATUS;
                    rescueRequest.Status = RescueRequestSettings.PENDING_STATUS;   
                    rescueTeam.CurrentStatus = RescueTeamSettings.AVAILABLE_STATUS;

                    _logger.LogInformation("[RescueMissionService] Rescue Mission with ID: {MissionID} declined - Team {TeamName} set to Available, Request set to Pending", request.RescueMissionID, rescueTeam.TeamName);

                    TeamRejectedMessage kafkaMessage = _mapper.Map<TeamRejectedMessage>(rescueMission);
                    _mapper.Map(rescueRequest, kafkaMessage);
                    _mapper.Map(rescueTeam, kafkaMessage);

                    await _kafkaProducer.ProduceAsync(topic: KafkaSettings.TEAM_REJECTED_TOPIC, key: rescueMission.RescueMissionID.ToString(), message: kafkaMessage);
                    
                    _logger.LogInformation("[RescueMissionService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.TEAM_REJECTED_TOPIC);
                }

                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogError("[RescueMissionService - Error] SaveChanges returned 0 rows during respond. MissionID: {MissionID}", request.RescueMissionID);  
                    await transaction.RollbackAsync();
                    return null;
                }

               

                _logger.LogInformation("[RescueMissionService] Successfully responded to mission with ID: {MissionID}", request.RescueMissionID);

                //mapper rescue misson -> respond mission response dto
                //mapper rescue request -> respond mission response dto 
                //mapper rescue team -> respond mission response dto    
                //mappper respond mission requesst dto -> respond mission response dto - map tay 

                RespondMissionResponseDTO response = _mapper.Map<RespondMissionResponseDTO>(rescueMission);
                _mapper.Map(rescueRequest, response);
                _mapper.Map(rescueTeam, response);

                response.RespondedAt = respondedAt; 
                response.Message = request.IsAccepted ? $"Rescue mission with ID {rescueMission.RescueMissionID} has been accepted by Team {rescueTeam.TeamName}." : $"Rescue mission with ID {rescueMission.RescueMissionID} has been declined by Team {rescueTeam.TeamName}.";

                await transaction.CommitAsync();

                return response;

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[RescueMissionService - Error] Respond failed. Transaction rolled back. MissionID: {MissionID}", request.RescueMissionID);
                throw;
            }
        }
    }
}
