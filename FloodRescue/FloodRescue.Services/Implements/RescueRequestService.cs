using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.RescueRequestRequest;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using FloodRescue.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements
{
    public class RescueRequestService : IRescueRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RescueRequestService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<(RescueRequestResponseDTO? data, string? errorMessage)> CreateRescueRequestAsync(RescueRequestRequestDTO request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.CoordinatorID ?? Guid.Empty);
            if (user == null)
            {
                return (null, "User not found");
            }
            if (user.RoleID != "RC") {
                return (null, "User is not a coordinator");
            }

            var rescueRequestEntity = _mapper.Map<RescueRequest>(request);
            await _unitOfWork.RescueRequests.AddAsync(rescueRequestEntity);
            await _unitOfWork.SaveChangesAsync();
            var response = await _unitOfWork.RescueRequests.GetAsync(r => r.RescueRequestID == rescueRequestEntity.RescueRequestID, r => r.Coordinator!);
            return  (_mapper.Map<RescueRequestResponseDTO>(response), null);
        }

        public async Task<bool> DeleteRescueRequestAsync(Guid id)
        {
            var rescueRequest = await _unitOfWork.RescueRequests.GetByIdAsync(id);
            if (rescueRequest == null)
            {
                return false;
            }
            rescueRequest.IsDeleted = true;
            _unitOfWork.RescueRequests.Update(rescueRequest);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<List<RescueRequestResponseDTO>> GetAllRescueRequestsAsync()
        {
            var rescueRequests = await _unitOfWork.RescueRequests.GetAllAsync(r => !r.IsDeleted, r => r.Coordinator!);
            return _mapper.Map<List<RescueRequestResponseDTO>>(rescueRequests);
        }

        public async Task<RescueRequestResponseDTO?> GetRescueRequestByIdAsync(Guid id)
        {
           RescueRequest? rescueRequest = await _unitOfWork.RescueRequests.GetAsync(r => r.RescueRequestID == id && !r.IsDeleted, r => r.Coordinator!);
            if (rescueRequest == null)
            {
                return null;
            }
            return _mapper.Map<RescueRequestResponseDTO>(rescueRequest);
        }

        public async Task<(RescueRequestResponseDTO? data, string? errorMessage)> UpdateRescueRequestAsync(Guid id, RescueRequestRequestDTO request)
        {
            // 1. Kiểm tra xem RescueRequest có tồn tại không
            RescueRequest? existingRequest = await _unitOfWork.RescueRequests.GetByIdAsync(id);
            if (existingRequest == null || existingRequest.IsDeleted)
            {
                return (null, "Rescue request not found."); // Hoặc ném ra ngoại lệ tùy theo yêu cầu của bạn
            }
            // 2. Kiểm tra quyền của CoordinatorID gửi lên
            var user = await _unitOfWork.Users.GetByIdAsync(request.CoordinatorID ?? Guid.Empty);
            if (user == null)
                return (null, "Coordinator not found.");

            if (user.RoleID != "RC")
                return (null, "Access denied. Only users with 'RC' role can update rescue requests.");
            // 3. Thực hiện cập nhật
            

            _mapper.Map(request, existingRequest);
            await _unitOfWork.SaveChangesAsync();

            var updateRequest = await _unitOfWork.RescueRequests.GetAsync(r => r.RescueRequestID == id && !r.IsDeleted, r => r.Coordinator!);
            return ( _mapper.Map<RescueRequestResponseDTO>(updateRequest), null);
        }
    }
}
