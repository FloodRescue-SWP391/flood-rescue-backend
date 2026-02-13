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
            _logger.LogInformation("[ReliefItemService] Creating new ReliefItem. Name: {ReliefItemName}", request.ReliefItemName);
            var item = _mapper.Map<ReliefItemEntity>(request);
            await _unitOfWork.ReliefItems.AddAsync(item);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("[ReliefItemService - Sql Server] Successfully created ReliefItem with ID: {ReliefItemId}", item.ReliefItemID);
            ReliefItemResponseDTO responseDTO = _mapper.Map<ReliefItemResponseDTO>(item);
            await _cacheService.RemoveAsync(ALL_RELIEF_ITEMS_KEY);
            _logger.LogInformation("[ReliefItemService - Redis] Cleared cache for All ReliefItems list.");
            return responseDTO;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("[ReliefItemService] Deleting ReliefItem with ID: {ReliefItemId}", id);
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id);
            if (item == null)
            {
                _logger.LogWarning("[ReliefItemService - Sql Server] Delete failed. ReliefItem ID: {ReliefItemId} not found.", id);
                return false;
            }
            if (!item.IsDeleted)
            {
                item.IsDeleted = true;
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("[ReliefItemService - Sql Server] Soft-deleted ReliefItem ID: {ReliefItemId}", id);
                // xóa 2 cache song song
                await Task.WhenAll(
                     _cacheService.RemoveAsync(ALL_RELIEF_ITEMS_KEY),
                     _cacheService.RemoveAsync(RELIEF_ITEM_KEY_PREFIX + id)
                );
                _logger.LogInformation("[ReliefItemService - Redis] Cleared cache for ReliefItem ID: {ReliefItemId} and list.", id);

                return true;
            }
            _logger.LogInformation("[ReliefItemService] ReliefItem ID: {ReliefItemId} already deleted. Skipped.", id);
            return false;
        }

        public async Task<List<ReliefItemResponseDTO>> GetAllAsync()
        {
            _logger.LogInformation("[ReliefItemService] Getting all ReliefItems.");
            var cache = await _cacheService.GetAsync<List<ReliefItemResponseDTO>>(ALL_RELIEF_ITEMS_KEY);
            if (cache != null)
            {
                _logger.LogInformation("[ReliefItemService - Redis] Cache hit for all ReliefItems.");
                return cache;
            }
            _logger.LogInformation("[ReliefItemService - Redis] Cache miss. Fetching from database.");
            var list = await _unitOfWork.ReliefItems.GetAllAsync(r => !r.IsDeleted, r => r.Category!);

            List<ReliefItemResponseDTO> responseDTO = _mapper.Map<List<ReliefItemResponseDTO>>(list);

            await _cacheService.SetAsync(ALL_RELIEF_ITEMS_KEY, responseDTO, TimeSpan.FromMinutes(10));
            _logger.LogInformation("[ReliefItemService - Redis] Cached {Count} ReliefItems.", responseDTO.Count);

            return responseDTO;
        }

        public async Task<ReliefItemResponseDTO?> GetByIdAsync(int id)
        {
            _logger.LogInformation("[ReliefItemService] Searching ReliefItem ID: {ReliefItemId}", id);
            var cache = await _cacheService.GetAsync<ReliefItemResponseDTO>(RELIEF_ITEM_KEY_PREFIX + id);
            if (cache != null)
            {
                _logger.LogInformation("[ReliefItemService - Redis] Cache hit for ReliefItem ID: {ReliefItemId}", id);
                return cache;
            }
            _logger.LogInformation("[ReliefItemService - Redis] Cache miss for ReliefItem ID: {ReliefItemId}", id);
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id && !r.IsDeleted, r => r.Category!);
            if (item != null)
            {
                ReliefItemResponseDTO responseDTO = _mapper.Map<ReliefItemResponseDTO>(item);
                await _cacheService.SetAsync(RELIEF_ITEM_KEY_PREFIX + id, responseDTO, TimeSpan.FromMinutes(5));
                _logger.LogInformation("[ReliefItemService - Redis] Cached ReliefItem ID: {ReliefItemId}", id);
                return responseDTO;
            }
            _logger.LogWarning("[ReliefItemService - Sql Server] ReliefItem ID: {ReliefItemId} not found.", id);
            return null;
        }

        public async Task<bool> UpdateAsync(int id, CreateReliefItemRequestDTO request)
        {
            _logger.LogInformation("[ReliefItemService] Updating ReliefItem ID: {ReliefItemId}", id);
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id && !r.IsDeleted);
            if (item == null) 
            {
                _logger.LogWarning("[ReliefItemService - Sql Server] Update failed. ReliefItem ID: {ReliefItemId} not found.", id);
                return false;
            }
            string oldName = item.ReliefItemName;
            _mapper.Map(request, item);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("[ReliefItemService - Sql Server] Updated ReliefItem ID: {Id}. Name: '{OldName}' -> '{NewName}'",
            id, oldName, request.ReliefItemName);
            await Task.WhenAll(
                _cacheService.RemoveAsync(ALL_RELIEF_ITEMS_KEY),       
                _cacheService.RemoveAsync($"{RELIEF_ITEM_KEY_PREFIX}{id}")  
            );
            _logger.LogInformation("[ReliefItemService - Redis] Cleared cache for ReliefItem ID: {ReliefItemId} and list.", id);
            return true;
        }
    }
}
