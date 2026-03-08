using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.Request.Tracking;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.SharedSetting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace FloodRescue.API.Controllers
{
    public class TrackingController : ControllerBase
    {
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TrackingController> _logger;

        public const string TEAM_MEMBER_CACHE_PREFIX = "teammember:user:";

        public TrackingController(IKafkaProducerService kafkaProducer, ICacheService cacheService, ILogger<TrackingController> logger)
        {
            _kafkaProducer = kafkaProducer;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Nhận tọa độ GPS từ Rescue Team member (5-10s/lần)
        /// Fire & Forget: đẩy vào Kafka rồi trả 200 OK ngay, không chờ xử lý
        /// Mục đích: API phản hồi < 50ms, không làm chai DB
        /// </summary>
        [HttpPost("location")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateLocation([FromBody] UpdateLocationRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<string>.Fail("Invalid location data", 400));   
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return Unauthorized(ApiResponse<string>.Fail("User ID claim is missing or invalid", 401));
                }

                var teamIdClaim = User.FindFirst("TeamID");

                if (teamIdClaim == null || !Guid.TryParse(teamIdClaim.Value, out Guid teamId))
                {
                    return Unauthorized(ApiResponse<string>.Fail("Team ID claim is missing or invalid", 401));
                }

                TeamLocationKafkaMessage kafkaMessage = new TeamLocationKafkaMessage
                {
                    TeamID = teamId,
                    RescueMissionID = request.RescueMissionID,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    ClientTimestamp = request.ClientTimestamp
                };

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.TEAM_LOCATION_TRACKING_TOPIC,
                    key: teamId.ToString(),
                    message: kafkaMessage
                    );

                _logger.LogDebug("[TrackingController] Location sent to Kafka. TeamID: {TeamID}, Lat: {Lat}, Lng: {Lng}",
                   teamId, request.Latitude, request.Longitude);

                return Ok(ApiResponse<string>.Ok("Location received.", "Location update accepted.", 200));

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[TrackingController - Error] Failed to process location update.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error.", 500));
            }
        }
        
    }
}
