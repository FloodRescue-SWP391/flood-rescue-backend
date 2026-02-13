using AutoMapper;
using Azure.Core;
using Confluent.Kafka;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.RescueTeamRequest;
using FloodRescue.Services.DTO.Response.RescueTeamResponse;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.RescueTeam;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RescueTeamEntity = FloodRescue.Repositories.Entites.RescueTeam;

namespace FloodRescue.Services.Implements.RescueTeam
{
    public class RescueTeamService : IRescueTeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<RescueTeamService> _logger;

        public RescueTeamService(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService, ILogger<RescueTeamService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
        }

        private const string ALL_RESCUETEAMS_KEY = "RescueTeams:all";
        private const string RESCUETEAM_KEY_PREFIX = "RescueTeam:";
        public async Task<RescueTeamResponseDTO> CreateRescueTeamAsync(RescueTeamRequestDTO rescueTeamDTO)
        {
            _logger.LogInformation("[RescueTeamService] Request to create new Rescue Team. Name: {RescueTeamName}", rescueTeamDTO.TeamName);
            RescueTeamEntity rescueTeam = _mapper.Map<RescueTeamEntity>(rescueTeamDTO);
            await _unitOfWork.RescueTeams.AddAsync(rescueTeam);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("[RescueTeamService - Sql Server] Successfully created Rescue Team with ID: {RescueTeamId}", rescueTeam.RescueTeamID);
            await _cacheService.RemoveAsync(ALL_RESCUETEAMS_KEY);
            _logger.LogInformation("[RescueTeamService - Redis] Cleared cache for All Rescue Teams list.");
            return _mapper.Map<RescueTeamResponseDTO>(rescueTeam);
        }

        public async Task<bool> DeleteRescueTeamAsync(Guid rescueTeamId)
        {
            _logger.LogInformation("[RescueTeamService] Request to delete Rescue Team ID: {RescueTeamId}", rescueTeamId);
            RescueTeamEntity? rescueTeam =  await _unitOfWork.RescueTeams.GetAsync(r => r.RescueTeamID == rescueTeamId);
            if (rescueTeam == null)
            {
                _logger.LogWarning("[RescueTeamService - Sql Server] Delete failed. Rescue Team with ID: {RescueTeamId} not found.", rescueTeamId);
                return false;
            }
            if (!rescueTeam.IsDeleted)
            {
                rescueTeam.IsDeleted = true;
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("[RescueTeamService - Sql Server] Successfully deleted Rescue Team ID: {RescueTeamId}", rescueTeamId);
                await Task.WhenAll(
                     _cacheService.RemoveAsync(RESCUETEAM_KEY_PREFIX + rescueTeamId),
                     _cacheService.RemoveAsync(ALL_RESCUETEAMS_KEY)
                    );
                _logger.LogInformation("[RescueTeamService - Redis] Cleared cache for Rescue Team ID: {RescueTeamId}", rescueTeamId);
                return true;
            }

            _logger.LogInformation("[RescueTeamService] RescueTeam ID: {RescueTeamId} was already marked as deleted. No changes made.", rescueTeamId);
            return false;
        }

        public async Task<List<RescueTeamResponseDTO>> GetAllRescueTeamsAsync()
        {
            _logger.LogInformation("[RescueTeamService] Getting all Rescue Teams");
            var cached = await _cacheService.GetAsync<List<RescueTeamResponseDTO>>(ALL_RESCUETEAMS_KEY);
            if (cached != null)
            {
                _logger.LogInformation("[RescueTeamService - Redis] Cache hit for all Rescue Teams.");
                return cached;
            }

            _logger.LogInformation("[RescueTeamService - Redis] Cache miss for all Rescue Teams. Fetching from database.");
            List<RescueTeamEntity> rescueTeams = await _unitOfWork.RescueTeams.GetAllAsync();

            List<RescueTeamResponseDTO> rescueTeamDTOs = _mapper.Map<List<RescueTeamResponseDTO>>(rescueTeams);
            _logger.LogInformation("[RescueTeamService - Sql Server] Retrieved {Count} rescue teams from database", rescueTeamDTOs.Count);
            await _cacheService.SetAsync(ALL_RESCUETEAMS_KEY, rescueTeamDTOs, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[RescueTeamService - Redis] Cached {Count} rescue teams", rescueTeamDTOs.Count);
            return rescueTeamDTOs;

        }

        public async Task<RescueTeamResponseDTO?> GetRescueTeamByIdAsync(Guid rescueTeamId)
        {
            _logger.LogInformation("[RescueTeamService] Searching for Rescue Team with ID: {RescueTeamID}", rescueTeamId);
            var cached = await _cacheService.GetAsync<RescueTeamResponseDTO>(RESCUETEAM_KEY_PREFIX + rescueTeamId);
            if (cached != null)
            {
                _logger.LogInformation("[RescueTeamService - Redis] Found Rescue Team in cache: {RescueTeamID}", rescueTeamId);
                return cached;
            }
            _logger.LogInformation("[RescueTeamService - Redis] Cache miss. Searching DB for Rescue Team with ID: {RescueTeamID}", rescueTeamId);
            RescueTeamResponseDTO? responseDTO = null;
            var dbResult = _unitOfWork.RescueTeams.GetAsync(r => r.RescueTeamID == rescueTeamId);
            if (dbResult != null)
            {
                responseDTO = _mapper.Map<RescueTeamResponseDTO>(dbResult);
                await _cacheService.SetAsync($"{RESCUETEAM_KEY_PREFIX}{rescueTeamId}", responseDTO, TimeSpan.FromMinutes(5));
                _logger.LogInformation("[RescueTeamService - Redis] Added Rescue Team to cache: {RescueTeamID}", rescueTeamId);
                return responseDTO;
            }

            _logger.LogWarning("[RescueTeamService - Sql Server] Rescue Team with ID: {RescueTeamID} not found in database.", rescueTeamId);
            return responseDTO;
        }

        public async Task<RescueTeamResponseDTO?> UpdateRescueTeamAsync(Guid rescueTeamId, RescueTeamRequestDTO rescueTeamDTO)
        {
            _logger.LogInformation("[RescueTeamService] Request to update Rescue Team ID: {RescueTeamId}", rescueTeamId);
            RescueTeamEntity? _rescueTeam  = await _unitOfWork.RescueTeams.GetAsync(r => r.RescueTeamID == rescueTeamId);
            if (_rescueTeam == null)
            {
                _logger.LogWarning("[RescueTeamService - Sql Server] Update failed. Rescue Team ID: {RescueTeamId} not found.", rescueTeamId);
                return null;
            }
            string oldName = _rescueTeam.TeamName;

            _mapper.Map(rescueTeamDTO, _rescueTeam);
            var result =   await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("[RescueTeamService - Sql Server] Successfully updated Rescue Team ID: {Id} changed name from '{OldName}' to '{NewName}'",
                rescueTeamId, oldName, rescueTeamDTO.TeamName);
            if (result > 0) 
            {
                _logger.LogInformation("[RescueTeamService - Sql Server] Successfully updated Rescue Team ID: {RescueTeamId} in database.", rescueTeamId);
                await Task.WhenAll(
                    _cacheService.RemoveAsync(RESCUETEAM_KEY_PREFIX + rescueTeamId),
                    _cacheService.RemoveAsync(ALL_RESCUETEAMS_KEY)
                );
                _logger.LogInformation("[RescueTeamService - Redis] Cleared cache for Rescue Team ID: {RescueTeamId} and List.", rescueTeamId);
                return _mapper.Map<RescueTeamResponseDTO>(_rescueTeam);
            }
            _logger.LogInformation("[RescueTeamService - Sql Server] No changes detected for Rescue Team ID: {RescueTeamId}.", rescueTeamId);
            return null;
        }
    }
}
