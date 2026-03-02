using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.ReliefOrderRequest;
using FloodRescue.Services.DTO.Response.ReliefOrder;
using FloodRescue.Services.DTO.Response.ReliefOrderResponse;
using FloodRescue.Services.Interface.ReliefOrder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

                return StatusCode(201, ApiResponse<ReliefOrderResponseDTO>.Ok(result, "Create relief order successfully", 201));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("[ReliefOrdersController - Error] Create Relief Orders Fail with Error {Error}", ex.Message);
                return StatusCode(400,ApiResponse<ReliefOrderResponseDTO>.Fail(ex.Message, 400));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[ReliefOrdersController - Error] Respond failed. RescueRequestID: {RequestID}", request.RescueRequestID);
                return StatusCode(500, ApiResponse<ReliefOrderResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Inventory Manager")]
        public async Task<ActionResult<ApiResponse<List<PendingOrderResponseDTO>>>> GetPendingOrders()
        {
            _logger.LogInformation("[ReliefOrdersController] GET pending relief orders called.");
            try
            {
                List<PendingOrderResponseDTO> result = await _service.GetPendingOrdersAsync();

                _logger.LogInformation("[ReliefOrdersController] Returned {Count} pending relief order(s).", result.Count);
                return Ok(ApiResponse<List<PendingOrderResponseDTO>>.Ok(result, "Get pending relief orders successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefOrdersController - Error] GET pending relief orders failed.");
                return StatusCode(500, ApiResponse<List<PendingOrderResponseDTO>>.Fail("Internal server error", 500));
        [Authorize(Roles = "Inventory Manager")]
        [HttpPut("prepare")]
        public async Task<ActionResult<ApiResponse<ReliefOrderResponseDTO>>> PrepareReliefOrder(PrepareOrderRequestDTO request)
        {
            _logger.LogInformation("[ReliefOrdersController] Start process request in prepare relief order controller");

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[ReliefOrdersController] Prepare validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<ReliefOrderResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }
                // Get ManagerID from JWT token
                var managerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value; // fallback

                if (string.IsNullOrEmpty(managerIdClaim) || !Guid.TryParse(managerIdClaim, out Guid managerID))
                {
                    _logger.LogWarning("[ReliefOrdersController] Cannot extract ManagerID from JWT token");
                    return Unauthorized(ApiResponse<ReliefOrderResponseDTO>.Fail("Unauthorized. Cannot identify manager.", 401));
                }
                _logger.LogInformation("[ReliefOrdersController] ManagerID from token: {ManagerID}", managerID);

                ReliefOrderResponseDTO? result = await _service.PrepareReliefOrderAsync(request, managerID);

                if (result == null)
                {
                    _logger.LogWarning("[ReliefOrdersController] Cannot Process Request in Prepare Relief Order Service with ReliefOrderID: {ID}", request.ReliefOrderID);
                    return NotFound(ApiResponse<ReliefOrderResponseDTO>.Fail("Cannot prepare Relief Order. Order not found or invalid status.", 404));
                }

                _logger.LogInformation("[ReliefOrdersController] Prepare Relief Order done successfully for ReliefOrderID: {ID}", request.ReliefOrderID);

                return Ok(ApiResponse<ReliefOrderResponseDTO>.Ok(result, "Prepare relief order successfully", 200));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("[ReliefOrdersController - Error] Prepare Relief Order failed with Error: {Error}", ex.Message);
                return StatusCode(400, ApiResponse<ReliefOrderResponseDTO>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefOrdersController - Error] Prepare Relief Order failed. ReliefOrderID: {OrderID}", request.ReliefOrderID);
                return StatusCode(500, ApiResponse<ReliefOrderResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}
