using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.ReliefOrderRequest;
using FloodRescue.Services.DTO.Response.ReliefOrder;
using FloodRescue.Services.Interface.ReliefOrder;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReliefOrdersController : ControllerBase
    {
        private readonly IReliefOrder _service;

        public ReliefOrdersController(IReliefOrder service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReliefOrderResponseDTO>>> Create(ReliefOrderRequestDTO request)
        {
            try
            {
                var result = await _service.CreateAsync(request);
                return ApiResponse<ReliefOrderResponseDTO>.Ok(result, "Create relief order successfully", 201);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<ReliefOrderResponseDTO>.Fail(ex.Message, 400);
            }
        }
    }
}
