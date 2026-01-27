using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.ReliefItem;
using FloodRescue.Services.DTO.Response.ReliefItem;
using FloodRescue.Services.Interface;

namespace FloodRescue.Services.Implements
{
    public class ReliefItemService : IReliefItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReliefItemService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ReliefItemResponseDTO> CreateAsync(CreateReliefItemRequestDTO request)
        {
            var item = _mapper.Map<ReliefItem>(request);
            await _unitOfWork.ReliefItems.AddAsync(item);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ReliefItemResponseDTO>(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id);
            if (item == null) return false;
            if (!item.IsDeleted)
            {
                item.IsDeleted = true;
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<ReliefItemResponseDTO>> GetAllAsync()
        {
            var list = await _unitOfWork.ReliefItems.GetAllAsync(r => !r.IsDeleted, r => r.Category!);
            return _mapper.Map<List<ReliefItemResponseDTO>>(list);
        }

        public async Task<ReliefItemResponseDTO?> GetByIdAsync(int id)
        {
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id && !r.IsDeleted, r => r.Category!);
            return item == null ? null : _mapper.Map<ReliefItemResponseDTO>(item);
        }

        public async Task<bool> UpdateAsync(int id, CreateReliefItemRequestDTO request)
        {
            var item = await _unitOfWork.ReliefItems.GetAsync(r => r.ReliefItemID == id && !r.IsDeleted);
            if (item == null) return false;
            item.ReliefItemName = request.ReliefItemName;
            item.CategoryID = request.CategoryID;
            item.Unit = request.Unit;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
