using AutoMapper;
using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RescueRequest;
using FloodRescue.Services.SharedSetting;
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
        /// <summary>
        /// Tạo yêu cầu cứu hộ mới
        /// Flow: Validate -> Save DB -> Kafka Produce -> Return ShortCode
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CreateRescueRequestResponseDTO>>> CreateRescueRequest(CreateRescueRequestDTO request)
        {
            _logger.LogInformation("API CreateRescueRequest called. Phone: {Phone}, Type: {Type}",
                request.PhoneNumber, request.RequestType);

            // 1. Gọi service tạo rescue request (validate + save DB trong transaction)
            var (data, errorMessage) = await _rescueRequestService.CreateRescueRequestAsync(request);

            if (data == null)
            {
                _logger.LogWarning("CreateRescueRequest failed: {Error}", errorMessage);
                return BadRequest(ApiResponse<CreateRescueRequestResponseDTO>.Fail(errorMessage ?? "Create rescue request failed"));
            }
            // 2. Trả về response với ShortCode
            _logger.LogInformation("CreateRescueRequest success. ShortCode: {ShortCode}", data.ShortCode);
            return Ok(ApiResponse<CreateRescueRequestResponseDTO>.Ok(data, "Create rescue request successfully", 201));
        }

        /// <summary>
        /// Citizen tra cứu trạng thái request bằng ShortCode
        /// </summary>
        [HttpGet("track/{shortCode}")]
        public async Task<ActionResult<ApiResponse<CreateRescueRequestResponseDTO>>> GetByShortCode(string shortCode)
        {
            _logger.LogInformation("API GetByShortCode called. ShortCode: {ShortCode}", shortCode);

            CreateRescueRequestResponseDTO? result = await _rescueRequestService.GetByShortCodeAsync(shortCode);
            if (result == null)
            {
                return NotFound(ApiResponse<CreateRescueRequestResponseDTO>.Fail("Rescue request not found", 404));
            }

            return Ok(ApiResponse<CreateRescueRequestResponseDTO>.Ok(result, "Get rescue request successfully", 200));
        }

        /// <summary>
        /// Coordinator xem danh sách tất cả rescue requests
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CreateRescueRequestResponseDTO>>>> GetAllRescueRequests()
        {
            _logger.LogInformation("API GetAllRescueRequests called.");

            List<CreateRescueRequestResponseDTO> result = await _rescueRequestService.GetAllRescueRequestsAsync();

            return Ok(ApiResponse<List<CreateRescueRequestResponseDTO>>.Ok(result, "Get all rescue requests successfully", 200));
        }
    }
}
