using AutoMapper;
using Confluent.Kafka;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using FloodRescue.Services.Implements.Kafka;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RescueRequest;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using RescueRequestEntity = FloodRescue.Repositories.Entites.RescueRequest;
namespace FloodRescue.Services.Implements.RescueRequest
{

    public class RescueRequestService : IRescueRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<RescueRequestService> _logger;
        private readonly IKafkaProducerService _kafkaProducerService;
        private readonly ICacheService _cacheService;
        // Cache keys
        private const string ALL_RESCUE_REQUESTS_KEY = "rescuerequest:all";
        private const string RESCUE_REQUEST_KEY_PREFIX = "rescuerequest:shortcode:";
        // Danh sách các RequestType hợp lệ lấy từ RescueRequestSetting
        private static readonly HashSet<string> ValidRequestTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            RescueRequestType.RESCUE_TYPE,
            RescueRequestType.SUPPLY_TYPE
        };
        public RescueRequestService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<RescueRequestService> logger, ICacheService cacheService, IKafkaProducerService kafkaProducerService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _kafkaProducerService = kafkaProducerService;
            _cacheService = cacheService;

        }
        public async Task<(CreateRescueRequestResponseDTO? Data, string? ErrorMessage)> CreateRescueRequestAsync(CreateRescueRequestDTO request)
        {
            _logger.LogInformation("[RescueRequestService] Creating new RescueRequest. Phone: {Phone}, Type: {Type}", request.PhoneNumber, request.RequestType);

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {

            if(request.RequestType == RescueRequestType.SUPPLY_TYPE && string.IsNullOrEmpty(request.Description))
            {
                _logger.LogInformation("[RescueRequestService] Invalid Rescue Request: Description  must not null if Rescue Request Type is Supply");
                return (null, "Invalid Rescue Request Type Supply. Description must be not null");
            }    

            // 1. Kiểm tra tính hợp lệ của RequestType (Rescue hoặc Supply)
            if (!ValidRequestTypes.Contains(request.RequestType))
            {
                _logger.LogWarning("[RescueRequestService] Invalid RequestType: {Type}. Valid types: {ValidTypes}",
                    request.RequestType, string.Join(", ", ValidRequestTypes));
                return (null, $"Invalid RequestType. Must be '{RescueRequestType.RESCUE_TYPE}' or '{RescueRequestType.SUPPLY_TYPE}'");
            }

            // 2. Kiêm tọa độ hợp lệ
            if (request.LocationLatitude < -90 || request.LocationLatitude > 90)
            {
                _logger.LogWarning("[RescueRequestService] Invalid Latitude: {Lat}", request.LocationLatitude);
                return (null, "Latitude must be between -90 and 90");
            }

            if (request.LocationLongitude < -180 || request.LocationLongitude > 180)
            {
                _logger.LogWarning("[RescueRequestService] Invalid Longitude: {Long}", request.LocationLongitude);
                return (null, "Longitude must be between -180 and 180");
            }
            // 3. Kiểm tra số điện thoại không được để trống
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                _logger.LogWarning("[RescueRequestService] Phone number is empty");
                return (null, "Phone number is required");
            }
            // ===== TẠO RESCUE REQUEST =====
            // 4. Tạo short code duy nhất (random + 4 số cuối SĐT)
            string shortCode = await GenerateUniqueShortCodeAsync();
            _logger.LogInformation("[RescueRequestService] Generated ShortCode: {ShortCode}", shortCode);

            // 5. Dùng AutoMapper map DTO -> Entity
            //    PhoneNumber -> CitizenPhone (đã config ForMember trong MappingProfile)
            //    ShortCode, Status, CreatedTime đã Ignore → service tự set
            RescueRequestEntity rescueRequest = _mapper.Map<RescueRequestEntity>(request);
            rescueRequest.ShortCode = shortCode;
            rescueRequest.Status = RescueRequestSettings.PENDING_STATUS;
            rescueRequest.CreatedTime = DateTime.UtcNow;
            // 6. Lưu rescueRequést vào DB
            await _unitOfWork.RescueRequests.AddAsync(rescueRequest);
            _logger.LogInformation("[RescueRequestService - Sql Server] Added RescueRequest to context. ID: {Id}", rescueRequest.RescueRequestID);

            // 7. Tạo và add các RescueRequestImage (xử lý collection ảnh nối bảng con)
            //    Mỗi URL tạo 1 row RescueRequestImage với FK trỏ về RescueRequest vừa tạo
            List<string> savedImageUrls = new();
            if (request.ImageUrls != null && request.ImageUrls.Count > 0)
            {
                foreach (string imageUrl in request.ImageUrls)
                {
                    if(string.IsNullOrWhiteSpace(imageUrl)) continue;
                    var image = new RescueRequestImage
                    {
                        ImageUrl = imageUrl,
                        RescueRequestID = rescueRequest.RescueRequestID
                    };
                    await _unitOfWork.RescueRequestImages.AddAsync(image);
                    savedImageUrls.Add(imageUrl);
                }
                _logger.LogInformation("[RescueRequestService - Sql Server] Added {Count} images for RescueRequest ID: {Id}",
                savedImageUrls.Count, rescueRequest.RescueRequestID);
            }
            // 8. SaveChanges — Unit of Work transaction (request + images save 1 lượt)
            int result = await _unitOfWork.SaveChangesAsync();

            if (result <= 0)
            {
                _logger.LogError("[RescueRequestService - Sql Server] Failed to save RescueRequest to database. ID: {Id}", rescueRequest.RescueRequestID);
                await transaction.RollbackAsync();
                return (null, "Failed to create rescue request");
            }

            // 9. Map Entity -> Response DTO (ImageUrls ignored trong profile → set thủ công)
            CreateRescueRequestResponseDTO responseDTO = _mapper.Map<CreateRescueRequestResponseDTO>(rescueRequest);
            responseDTO.ImageUrls = savedImageUrls;

            // 10. Invalidate cache list vì có data mới
            await _cacheService.RemoveAsync(ALL_RESCUE_REQUESTS_KEY);
            _logger.LogInformation("[RescueRequestService - Redis] Cleared cache for All RescueRequests list.");

            // 11. Cache luôn request mới theo ShortCode (citizen sẽ tra cứu ngay)
            await _cacheService.SetAsync($"{RESCUE_REQUEST_KEY_PREFIX}{shortCode}", responseDTO, TimeSpan.FromMinutes(10));
            _logger.LogInformation("[RescueRequestService - Redis] Cached new RescueRequest with ShortCode: {ShortCode}", shortCode);

            // 12. Kafka Produce - bắn message lên topic để consumer xử lí (SMS, notification, ...)
          
              
                //new RescueRequestKafkaMessage
                //{
                //    RescueRequestID = data.RescueRequestID,
                //    ShortCode = data.ShortCode,
                //    CitizenPhone = data.CitizenPhone,
                //    RequestType = data.RequestType,
                //    LocationLatitude = data.LocationLatitude,
                //    LocationLongitude = data.LocationLongitude,
                //    CreatedTime = data.CreatedTime
                //};

                // Key = RescueRequestID để Kafka partition theo request
            
                RescueRequestKafkaMessage kafkaMessage = _mapper.Map<RescueRequestKafkaMessage>(rescueRequest);

                await _kafkaProducerService.ProduceAsync(
                    KafkaSettings.RESCUE_REQUEST_CREATED_TOPIC, // topic
                    rescueRequest.RescueRequestID.ToString(), // key
                    kafkaMessage // event/message(object)
                );

                await transaction.CommitAsync();

                _logger.LogInformation("[RescueRequestService - Kafka Producer] Message produced to topic: {Topic} for RescueRequest ID: {Id}",
                    KafkaSettings.RESCUE_REQUEST_TOPIC, rescueRequest.RescueRequestID);

                return (responseDTO, null);
            }
            catch (Exception ex)
            {
                // Kafka fail không nên block response - request đã được lưu DB thành công
                await transaction.RollbackAsync();
                _logger.LogError("[RescueRequestService - Error] Transaction rolled back. Failed to create RescueRequest: {Error}", ex.Message);
                throw;
            }
           
        }



        public async Task<CreateRescueRequestResponseDTO?> GetByShortCodeAsync(string shortCode)
        {
            _logger.LogInformation("[RescueRequestService] Searching for RescueRequest with ShortCode: {ShortCode}", shortCode);
            // 1. Kiểm tra cache trước
            var cached = await _cacheService.GetAsync<CreateRescueRequestResponseDTO>($"{RESCUE_REQUEST_KEY_PREFIX}{shortCode}");
            if (cached != null)
            {
                _logger.LogInformation("[RescueRequestService - Redis] Cache hit for RescueRequest ShortCode: {ShortCode}", shortCode);
                return cached;
            }
            // 2. Nếu không có trong cache, truy vấn DB
            _logger.LogInformation("[RescueRequestService - Redis] Cache miss. Querying DB for RescueRequest ShortCode: {ShortCode}", shortCode);
            RescueRequestEntity? entity = await _unitOfWork.RescueRequests.GetAsync(r => r.ShortCode == shortCode && !r.IsDeleted);
            if (entity == null)
            {
                _logger.LogWarning("[RescueRequestService - Sql Server] RescueRequest with ShortCode: {ShortCode} not found in database.", shortCode);
                return null;
            }
            // 3. Lấy danh sách ảnh từ bảng con
            List<RescueRequestImage> images = await _unitOfWork.RescueRequestImages
                .GetAllAsync(img => img.RescueRequestID == entity.RescueRequestID);
            // 4. Map Entity -> Response DTO
            CreateRescueRequestResponseDTO responseDTO = _mapper.Map<CreateRescueRequestResponseDTO>(entity);
            responseDTO.ImageUrls = images.Select(img => img.ImageUrl).ToList();
            // 5. Lưu vào cache
            await _cacheService.SetAsync($"{RESCUE_REQUEST_KEY_PREFIX}{shortCode}", responseDTO, TimeSpan.FromMinutes(10));
            _logger.LogInformation("[RescueRequestService - Redis] Cached RescueRequest ShortCode: {ShortCode}", shortCode);

            return responseDTO;
        }

        public async Task<List<CreateRescueRequestResponseDTO>> GetAllRescueRequestsAsync()
        {
            _logger.LogInformation("[RescueRequestService] Getting all RescueRequests");
            var cached = await _cacheService.GetAsync<List<CreateRescueRequestResponseDTO>>(ALL_RESCUE_REQUESTS_KEY);
            if (cached != null) 
            {
                _logger.LogInformation("[RescueRequestService - Redis] Cache hit for all RescueRequests. Count: {Count}", cached.Count);
                return cached;
            }
            // 2. Cache miss → tìm trong DB
            _logger.LogInformation("[RescueRequestService - Redis] Cache miss for all RescueRequests. Fetching from database.");
            List<RescueRequestEntity> entities = await _unitOfWork.RescueRequests.GetAllAsync(r => !r.IsDeleted);
            // 3. Map từng entity + lấy ảnh từ bảng con
            List<CreateRescueRequestResponseDTO> responseDTOs = new();
            foreach (var entity in entities)
            {
                CreateRescueRequestResponseDTO dto = _mapper.Map<CreateRescueRequestResponseDTO>(entity);
                List<RescueRequestImage> images = await _unitOfWork.RescueRequestImages
                    .GetAllAsync(img => img.RescueRequestID == entity.RescueRequestID);
                dto.ImageUrls = images.Select(img => img.ImageUrl).ToList();
                responseDTOs.Add(dto);
            }
            _logger.LogInformation("[RescueRequestService - Sql Server] Retrieved {Count} rescue requests from database", responseDTOs.Count);

            // 4. Lưu vào cache
            await _cacheService.SetAsync(ALL_RESCUE_REQUESTS_KEY, responseDTOs, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[RescueRequestService - Redis] Cached {Count} rescue requests", responseDTOs.Count);

            return responseDTOs;
        }



        /// <summary>
        /// Sinh ShortCode unique dạng "FR-XXXXXX" (6 ký tự alphanumeric)
        /// Loop đến khi không trùng trong DB
        /// </summary>
        private async Task<string> GenerateUniqueShortCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string shortCode;
            int maxRetries = 10;
            int attempt = 0;
            int codeLength = 10;

            do
            {
                attempt++;
                // Tạo chuỗi ngẫu nhiên hoàn toàn
                shortCode = new string(Enumerable.Range(0, codeLength)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());


                // Check trùng trong DB
                var existing = await _unitOfWork.RescueRequests.GetAsync(r => r.ShortCode == shortCode);
                // Nếu không trùng -> Trả về kết quả ngay
                if (existing == null)
                {
                    return shortCode;
                }

                _logger.LogWarning("[RescueRequestService - Sql Server] ShortCode collision: {ShortCode}, retrying... (attempt {Attempt})", shortCode, attempt);

            } while (attempt < maxRetries);
            // Fallback: Nếu random 10 lần vẫn trùng (xác suất cực thấp), dùng Timestamp
            // Lấy Ticks hiện tại để đảm bảo duy nhất, cắt lấy 10 ký tự cuối
            shortCode = DateTime.UtcNow.Ticks.ToString();
            shortCode = shortCode.Length > codeLength 
                ? shortCode[^codeLength..]  // Lấy 10 số cuối của Ticks
                : shortCode.PadRight(codeLength, '0');
            _logger.LogWarning("[RescueRequestService] Max retries reached. Using timestamp-based ShortCode: {ShortCode}", shortCode);
            return shortCode;
        }
    }
}
