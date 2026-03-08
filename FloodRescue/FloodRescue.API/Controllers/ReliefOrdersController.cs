using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.ReliefOrderRequest;
using FloodRescue.Services.DTO.Response.ReliefOrder;
using FloodRescue.Services.DTO.Response.ReliefOrderResponse;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
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
        [Authorize(Roles = "Rescue Coordinator")]
        public async Task<ActionResult<ApiResponse<ReliefOrderResponseDTO>>> CreateReliefOrder(ReliefOrderRequestDTO request)
        {
            _logger.LogInformation("[ReliefOrdersController] Start process request in relief order controller");
            
            try
            {
                // 1. Lấy UserID từ JWT Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid currentUserId))
                {
                    _logger.LogWarning("[RescueMissionController] Unable to extract UserID from JWT token.");
                    return Unauthorized(ApiResponse<List<PendingMissionResponseDTO>>.Fail("Invalid token. Please login again.", 401));
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[ReliefOrdersController] Respond validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<ReliefOrderResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                ReliefOrderResponseDTO? result = await _service.CreateReliefOrderAsync(request, currentUserId);

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
        [Authorize(Roles = "Inventory Manager, Rescue Coordinator")]
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
            }
        }

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

        /// <summary>
        /// Lấy chi tiết phiếu xuất kho theo ID — gồm thông tin vỏ phiếu và danh sách hàng hóa bên trong
        /// GET /api/ReliefOrders/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Inventory Manager")]
        public async Task<ActionResult<ApiResponse<ReliefOrderDetailResponseDTO>>> GetOrderDetail(Guid id)
        {
            _logger.LogInformation("[ReliefOrdersController] GET order detail called. ReliefOrderID: {ID}", id);
            try
            {
                ReliefOrderDetailResponseDTO? result = await _service.GetOrderDetailAsync(id);

                if (result == null)
                {
                    _logger.LogWarning("[ReliefOrdersController] ReliefOrder not found. ID: {ID}", id);
                    return NotFound(ApiResponse<ReliefOrderDetailResponseDTO>.Fail("Relief Order not found.", 404));
                }

                _logger.LogInformation("[ReliefOrdersController] GetOrderDetail success. ReliefOrderID: {ID}, Items: {Count}", id, result.Items.Count);
                return Ok(ApiResponse<ReliefOrderDetailResponseDTO>.Ok(result, "Get relief order detail successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefOrdersController - Error] GET order detail failed. ReliefOrderID: {ID}", id);
                return StatusCode(500, ApiResponse<ReliefOrderDetailResponseDTO>.Fail("Internal server error", 500));
            }
        }
        /// Lấy danh sách Relief Orders có lọc theo trạng thái, thời gian và phân trang
        /// GET /api/ReliefOrders/filter?statuses=Pending&amp;statuses=Prepared&amp;pageNumber=1&amp;pageSize=10
        /// </summary>
        [HttpGet("filter")]
        [Authorize(Roles = "Inventory Manager")]
        public async Task<ActionResult<ApiResponse<PagedResult<ReliefOrderListResponseDTO>>>> GetFilteredOrders([FromQuery] ReliefOrderFilterDTO filter)
        {
            _logger.LogInformation("[ReliefOrdersController] GET filter relief orders called. Statuses: {Statuses}, Page: {Page}, Size: {Size}",
                filter.Statuses != null ? string.Join(",", filter.Statuses) : "All",
                filter.PageNumber, filter.PageSize);
            try
            {
                PagedResult<ReliefOrderListResponseDTO> result = await _service.GetFilteredOrdersAsync(filter);

                _logger.LogInformation("[ReliefOrdersController] GetFilteredOrders success. DataCount: {Count}, TotalCount: {Total}", result.Data.Count, result.TotalCount);
                return Ok(ApiResponse<PagedResult<ReliefOrderListResponseDTO>>.Ok(result, "Get filtered relief orders successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefOrdersController - Error] GET filter relief orders failed.");
                return StatusCode(500, ApiResponse<PagedResult<ReliefOrderListResponseDTO>>.Fail("Internal server error", 500));
            }
        }
    }
}
