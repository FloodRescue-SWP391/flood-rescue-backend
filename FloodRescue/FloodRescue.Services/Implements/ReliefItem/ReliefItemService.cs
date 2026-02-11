using AutoMapper;
using Azure;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.ReliefItem;
using FloodRescue.Services.DTO.Response.ReliefItem;
using FloodRescue.Services.Implements.Cache;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.ReliefItem;
using Microsoft.Extensions.Logging;
using ReliefItemEntity = FloodRescue.Repositories.Entites.ReliefItem;

namespace FloodRescue.Services.Implements.ReliefItem
{
    public class ReliefItemService : IReliefItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ReliefItemService> _logger;
        private readonly ICacheService _cacheService;

        public ReliefItemService(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService, ILogger<ReliefItemService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }
        private const string ALL_RELIEF_ITEMS_KEY = "reliefitem:all";
        private const string RELIEF_ITEM_KEY_PREFIX = "reliefitem:";
        public async Task<ReliefItemResponseDTO> CreateAsync(CreateReliefItemRequestDTO request)
        {
            _logger.LogInformation("Request to create new ReliefItem. Name: {ReliefItemName}", request.ReliefItemName);
            var item = _mapper.Map<ReliefItemEntity>(request);
            await _unitOfWork.ReliefItems.AddAsync(item);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully created ReliefItem with ID: {ReliefItemId}", item.ReliefItemID);
            ReliefItemResponseDTO responseDTO = _mapper.Map<ReliefItemResponseDTO>(item);
            await _cacheService.RemoveAsync(ALL_RELIEF_ITEMS_KEY);
            _logger.LogInformation("Cleared cache for All ReliefItems list.");
            return responseDTO;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Request to delete ReliefItem with ID: {ReliefItemId}", id);
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id);
            if (item == null)
            {
                _logger.LogWarning("Delete failed. ReliefItem with ID: {ReliefItemId} not found.", id);
                return false;
            }
            if (!item.IsDeleted)
            {
                item.IsDeleted = true;
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Successfully soft-deleted ReliefItem ID: {ReliefItemId} in database.", id);
                // xóa 2 cache song song
                await Task.WhenAll(
                     _cacheService.RemoveAsync(ALL_RELIEF_ITEMS_KEY),
                     _cacheService.RemoveAsync(RELIEF_ITEM_KEY_PREFIX + id)
                );
                _logger.LogInformation("Cleared cache for ReliefItem ID: {ReliefItemId} and List.", id);

                return true;
            }
            _logger.LogInformation("ReliefItem ID: {ReliefItemId} was already marked as deleted. No changes made.", id);
            return false;
        }

        public async Task<List<ReliefItemResponseDTO>> GetAllAsync()
        {
            _logger.LogInformation("Searching for all ReliefItems.");
            var cache = await _cacheService.GetAsync<List<ReliefItemResponseDTO>>(ALL_RELIEF_ITEMS_KEY);
            if (cache != null)
            {
                _logger.LogInformation("Retrieved all ReliefItems from cache.");
                return cache;
            }
            _logger.LogInformation("Cache miss. Querying database for all ReliefItems.");
            var list = await _unitOfWork.ReliefItems.GetAllAsync(r => !r.IsDeleted, r => r.Category!);

            List<ReliefItemResponseDTO> responseDTO = _mapper.Map<List<ReliefItemResponseDTO>>(list);

            await _cacheService.SetAsync(ALL_RELIEF_ITEMS_KEY, responseDTO, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Added all ReliefItems to cache.");

            return responseDTO;
        }

        public async Task<ReliefItemResponseDTO?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Searching for ReliefItem with ID: {ReliefItemId}", id);
            var cache = await _cacheService.GetAsync<ReliefItemResponseDTO>(RELIEF_ITEM_KEY_PREFIX + id);
            if (cache != null)
            {
                _logger.LogInformation("Found ReliefItem in cache: {ReliefItemId}", id);
                return cache;
            }
            _logger.LogInformation("Cache miss. Querying database for ReliefItem ID: {ReliefItemId}", id);
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id && !r.IsDeleted, r => r.Category!);
            if (item != null)
            {
                ReliefItemResponseDTO responseDTO = _mapper.Map<ReliefItemResponseDTO>(item);
                await _cacheService.SetAsync(RELIEF_ITEM_KEY_PREFIX + id, responseDTO, TimeSpan.FromMinutes(5));
                _logger.LogInformation("Added ReliefItem ID: {ReliefItemId} to cache.", id);
                return responseDTO;
            }
            _logger.LogWarning("ReliefItem with ID: {ReliefItemId} not found in database.", id);
            return null;
        }

        public async Task<bool> UpdateAsync(int id, CreateReliefItemRequestDTO request)
        {
            _logger.LogInformation("Request to update ReliefItem ID: {ReliefItemId}", id);
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id && !r.IsDeleted);
            if (item == null) 
            {
                _logger.LogWarning("Update failed. ReliefItem ID: {ReliefItemId} not found.", id);
                return false;
            }
            string oldName = item.ReliefItemName;
            _mapper.Map(request, item);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully updated ReliefItem ID: {Id} changed name from '{OldName}' to '{NewName}'",
            id, oldName, request.ReliefItemName);
            await Task.WhenAll(
                _cacheService.RemoveAsync(ALL_RELIEF_ITEMS_KEY),       
                _cacheService.RemoveAsync($"{RELIEF_ITEM_KEY_PREFIX}{id}")  
            );
            _logger.LogInformation("Cleared cache for ReliefItem ID: {ReliefItemId} and List.", id);
            return true;
        }
    }
}
