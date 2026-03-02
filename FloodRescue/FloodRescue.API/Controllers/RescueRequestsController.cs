using AutoMapper;
using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RescueRequest;
using FloodRescue.Services.SharedSetting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RescueRequestsController : ControllerBase
    {
        private readonly IRescueRequestService _rescueRequestService;
        private readonly ILogger<RescueRequestsController> _logger;


        public RescueRequestsController(IRescueRequestService rescueRequestService,IKafkaProducerService kafkaProducerService,ILogger<RescueRequestsController> logger)
        {
            _rescueRequestService = rescueRequestService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CreateRescueRequestResponseDTO>>> CreateRescueRequest(CreateRescueRequestDTO request)
        {
            _logger.LogInformation("[RescueRequestsController] POST create rescue request called. Phone: {Phone}, Type: {Type}",
                request.PhoneNumber, request.RequestType);
            try
            {
                if(request.RequestType  == RescueRequestType.SUPPLY_TYPE && string.IsNullOrEmpty(request.Description))
                {
                    _logger.LogWarning("[RescueRequestsController] Create Rescue Request Fail - Description must not be null if rescue type is supply");
                    return BadRequest(ApiResponse<CreateRescueRequestResponseDTO>.Fail("Description must not be null if rescue type is supply"));
                }

                var (data, errorMessage) = await _rescueRequestService.CreateRescueRequestAsync(request);

                if (data == null)
                {
                    _logger.LogWarning("[RescueRequestsController] Create rescue request failed. Error: {Error}", errorMessage);
                    return BadRequest(ApiResponse<CreateRescueRequestResponseDTO>.Fail(errorMessage ?? "Create rescue request failed"));
                }

                _logger.LogInformation("[RescueRequestsController] Rescue request created. ShortCode: {ShortCode}, ID: {Id}", data.ShortCode, data.RescueRequestID);
                return Ok(ApiResponse<CreateRescueRequestResponseDTO>.Ok(data, "Create rescue request successfully", 201));
                
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[RescueRequestsController - Error] Create rescue request failed. Phone: {Phone}, Type: {Type}", request.PhoneNumber, request.RequestType);
                return StatusCode(500, ApiResponse<CreateRescueRequestResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpGet("track/{shortCode}")]
        public async Task<ActionResult<ApiResponse<CreateRescueRequestResponseDTO>>> GetByShortCode(string shortCode)
        {
            _logger.LogInformation("[RescueRequestsController] GET track called. ShortCode: {ShortCode}", shortCode);
            try
            {
                CreateRescueRequestResponseDTO? result = await _rescueRequestService.GetByShortCodeAsync(shortCode);
                if (result == null)
                {
                    _logger.LogWarning("[RescueRequestsController] Rescue request not found. ShortCode: {ShortCode}", shortCode);
                    return NotFound(ApiResponse<CreateRescueRequestResponseDTO>.Fail("Rescue request not found", 404));
                }

                _logger.LogInformation("[RescueRequestsController] Rescue request found. ShortCode: {ShortCode}", shortCode);
                return Ok(ApiResponse<CreateRescueRequestResponseDTO>.Ok(result, "Get rescue request successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueRequestsController - Error] GET track failed. ShortCode: {ShortCode}", shortCode);
                return StatusCode(500, ApiResponse<CreateRescueRequestResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CreateRescueRequestResponseDTO>>>> GetAllRescueRequests()
        {
            _logger.LogInformation("[RescueRequestsController] GET all rescue requests called.");
            try
            {
                List<CreateRescueRequestResponseDTO> result = await _rescueRequestService.GetAllRescueRequestsAsync();
                _logger.LogInformation("[RescueRequestsController] Returned {Count} rescue requests.", result.Count);
                return Ok(ApiResponse<List<CreateRescueRequestResponseDTO>>.Ok(result, "Get all rescue requests successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueRequestsController - Error] GET all rescue requests failed.");
                return StatusCode(500, ApiResponse<List<CreateRescueRequestResponseDTO>>.Fail("Internal server error", 500));
            }
        }

        /// <summary>
        /// [PUBLIC API] Citizen tra cứu trạng thái yêu cầu cứu trợ bằng mã ShortCode
        /// Không cần đăng nhập - Thông tin được che giấu để bảo vệ quyền riêng tư
        /// GET /api/rescuerequests/track?shortCode=FR-1234
        /// </summary>
        [HttpGet("track")]
        [AllowAnonymous]  // Không cần đăng nhập
        public async Task<ActionResult<ApiResponse<TrackRequestResponseDTO>>> TrackRequest([FromQuery] string shortCode)
        {
            _logger.LogInformation("[RescueRequestsController] GET track request called. ShortCode: {ShortCode}", shortCode);

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(shortCode))
                {
                    _logger.LogWarning("[RescueRequestsController] ShortCode is empty.");
                    return BadRequest(ApiResponse<TrackRequestResponseDTO>.Fail("Mã tra cứu không được để trống", 400));
                }

                // Gọi service
                var (data, errorMessage) = await _rescueRequestService.TrackRequestByShortCodeAsync(shortCode);

                // Xử lý kết quả
                if (errorMessage != null)
                {
                    _logger.LogWarning("[RescueRequestsController] TrackRequest failed: {Error}", errorMessage);
                    return NotFound(ApiResponse<TrackRequestResponseDTO>.Fail(errorMessage, 404));
                }

                _logger.LogInformation("[RescueRequestsController] TrackRequest success. ShortCode: {ShortCode}", shortCode);
                return Ok(ApiResponse<TrackRequestResponseDTO>.Ok(data!, "Tra cứu thành công", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueRequestsController - Error] TrackRequest failed. ShortCode: {ShortCode}", shortCode);
                return StatusCode(500, ApiResponse<TrackRequestResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}
