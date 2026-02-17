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
        private readonly ILogger<ReliefOrdersController> _logger;
        public ReliefOrdersController(IReliefOrder service, ILogger<ReliefOrdersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReliefOrderResponseDTO>>> CreateReliefOrder(ReliefOrderRequestDTO request)
        {
            _logger.LogInformation("[ReliefOrdersController] Start process request in relief order controller");
            
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[ReliefOrdersController] Respond validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<ReliefOrderResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                ReliefOrderResponseDTO? result = await _service.CreateReliefOrderAsync(request);

                if(result == null)
                {
                    _logger.LogWarning("[ReliefOrdersController] Cannot Process Request in Create Relief Order Service with request object {@dto}", request);
                    return NotFound(ApiResponse<ReliefOrderResponseDTO>.Fail("Cannot Create Relief Order", 404));
                }

                _logger.LogInformation("[ReliefOrdersController] Service done successfully");

                return ApiResponse<ReliefOrderResponseDTO>.Ok(result, "Create relief order successfully", 201);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("[ReliefOrdersController - Error] Create Relief Orders Fail with Error {Error}", ex.Message);
                return ApiResponse<ReliefOrderResponseDTO>.Fail(ex.Message, 400);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[ReliefOrdersController - Error] Respond failed. RescueRequestID: {RequestID}", request.RescueRequestID);
                return StatusCode(500, ApiResponse<ReliefOrderResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}
