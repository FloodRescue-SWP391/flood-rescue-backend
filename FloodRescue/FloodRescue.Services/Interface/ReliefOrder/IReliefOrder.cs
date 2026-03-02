using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FloodRescue.Services.DTO.Request.ReliefOrderRequest;
using FloodRescue.Services.DTO.Response.ReliefOrder;
using FloodRescue.Services.DTO.Response.ReliefOrderResponse;

namespace FloodRescue.Services.Interface.ReliefOrder
{
    public interface IReliefOrder
    {
        Task<ReliefOrderResponseDTO?> CreateReliefOrderAsync(ReliefOrderRequestDTO request);
        Task<List<PendingOrderResponseDTO>> GetPendingOrdersAsync();
        Task<ReliefOrderResponseDTO?> PrepareReliefOrderAsync(PrepareOrderRequestDTO request, Guid managerID);
    }
}
