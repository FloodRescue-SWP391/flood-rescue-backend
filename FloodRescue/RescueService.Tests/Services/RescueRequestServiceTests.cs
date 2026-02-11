using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using FloodRescue.Services.Implements.Kafka;
using FloodRescue.Services.Implements.RescueRequest;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Mapper;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FloodRescue.Tests.Services
{
    // TestFixture : đánh dấu là class chứa các test case , NUnit sẽ dựa vào đó để tìm và chạy test
    [TestFixture]
    public class RescueRequestServiceTests
    {
        // Khai báo các Mock (đối tượng giả)
        private Mock<IUnitOfWork> _uowMock = null!;
        private Mock<IBaseRepository<RescueRequest>> _rescueRequestRepoMock = null!;
        private Mock<IBaseRepository<RescueRequestImage>> _rescueRequestImageRepoMock = null!;
        private Mock<ICacheService> _cacheMock = null!;
        private Mock<IKafkaProducerService> _kafkaMock = null!;
        private Mock<ILogger<RescueRequestService>> _loggerMock = null!;

        private IMapper _mapper = null!;
        private RescueRequestService _service = null!;

        private const string ALL_KEY = "rescuerequest:all";
        private const string PREFIX = "rescuerequest:shortcode:";

        [SetUp] // Hàm này chạy TRƯỚC MỖI test case
        public void Setup()
        {
            // 1. MockBehavior.Strict: Chế độ nghiêm ngặt. 
            // Nếu Service gọi hàm nào của Mock mà bạn chưa .Setup(), test sẽ báo lỗi ngay lập tức.
            _uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _rescueRequestRepoMock = new Mock<IBaseRepository<RescueRequest>>(MockBehavior.Strict);
            _rescueRequestImageRepoMock = new Mock<IBaseRepository<RescueRequestImage>>(MockBehavior.Strict);
            _cacheMock = new Mock<ICacheService>(MockBehavior.Strict);
            _kafkaMock = new Mock<IKafkaProducerService>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<RescueRequestService>>();

            _uowMock.SetupGet(x => x.RescueRequests).Returns(_rescueRequestRepoMock.Object);
            _uowMock.SetupGet(x => x.RescueRequestImages).Returns(_rescueRequestImageRepoMock.Object);
            // 2. Cấu hình Mapper thực tế (Không mock Mapper vì logic map cần test thật)
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();

                // IMPORTANT: Service đang map rescueRequest(entity) -> RescueRequestKafkaMessage
                // nhưng MappingProfile bạn gửi thiếu map này => phải add để test & runtime không nổ.

                cfg.CreateMap<RescueRequest, RescueRequestKafkaMessage>();
            });
            _mapper = mapperConfig.CreateMapper();
            // 3. Khởi tạo Service cần test (SUT - System Under Test) với các dependency giả
            _service = new RescueRequestService(
                _uowMock.Object,
                _mapper,
                _loggerMock.Object,
                _cacheMock.Object,
                _kafkaMock.Object
            );
        }

        private static CreateRescueRequestDTO ValidDto(List<string>? urls = null)
        {
            return new CreateRescueRequestDTO
            {
                RequestType = RescueRequestType.RESCUE_TYPE,
                Description = "Help",
                LocationLatitude = 10.77,
                LocationLongitude = 106.69,
                PhoneNumber = "0909123456",
                ImageUrls = urls ?? new List<string> { "https://img/1.png", "https://img/2.png" }
            };
        }

        // =========================
        // CreateRescueRequestAsync
        // =========================

        [Test]
        public async Task CreateRescueRequestAsync_InvalidRequestType_ReturnsError_AndNoSideEffects()
        {
            // Arrange (Chuẩn bị)
            var dto = ValidDto();
            dto.RequestType = "INVALID"; // Cố tình làm sai
            // Act (Hành động)
            var (data, err) = await _service.CreateRescueRequestAsync(dto);

            // Assert (Kiểm tra)
            Assert.IsNull(data);// Dữ liệu trả về phải null
            Assert.IsNotNull(err); // Phải có lỗi

            StringAssert.Contains("Invalid RequestType", err);
            // Verify (Quan trọng): Đảm bảo không có lệnh gọi DB nào được thực thi
            //Mục tiêu quan trọng nhất ở đây không chỉ là trả về lỗi, mà là đảm bảo DB không bị gọi (VerifyNoOtherCalls). Nếu validate sai mà vẫn gọi db.AddAsync là Bug nghiêm trọng
            _uowMock.VerifyNoOtherCalls();
            _rescueRequestRepoMock.VerifyNoOtherCalls();
            _rescueRequestImageRepoMock.VerifyNoOtherCalls();
            _cacheMock.VerifyNoOtherCalls();
            _kafkaMock.VerifyNoOtherCalls();
        }

        [Test]
        // Case 1: Vĩ độ (Latitude) sai (quá 90)
        [TestCase(91, 106.69, "Latitude must be between -90 and 90")]
        // Case 2: Vĩ độ (Latitude) sai (dưới -90)
        [TestCase(-91, 106.69, "Latitude must be between -90 and 90")]
        // Case 3: Kinh độ (Longitude) sai (quá 180)
        [TestCase(10.77, 181, "Longitude must be between -180 and 180")]
        // Case 4: Kinh độ (Longitude) sai (dưới -180)
        [TestCase(10.77, -181, "Longitude must be between -180 and 180")]
        public async Task CreateRescueRequestAsync_InvalidCoordinates_ReturnsError(double lat, double lon, string expectedErrorMsg)
        {
            // 1. Arrange: Tạo DTO chuẩn trước
            var dto = ValidDto();

            // Ghi đè Latitude và Longitude bằng tham số truyền vào từ TestCase
            dto.LocationLatitude = lat;
            dto.LocationLongitude = lon;

            // 2. Act: Gọi service
            var (data, err) = await _service.CreateRescueRequestAsync(dto);

            // 3. Assert: Kiểm tra kết quả
            Assert.IsNull(data, "Data should be null when coordinates are invalid");
            Assert.IsNotNull(err, "Error message should not be null");

            // Kiểm tra xem thông báo lỗi trả về có đúng như mong đợi không
            Assert.AreEqual(expectedErrorMsg, err);

            // 4. Verify: Đảm bảo không gọi DB hay Kafka
            _uowMock.VerifyNoOtherCalls();
            _cacheMock.VerifyNoOtherCalls();
            _kafkaMock.VerifyNoOtherCalls();
        }


        [TestCase("")]
        [TestCase("   ")]
        public async Task CreateRescueRequestAsync_EmptyPhone_ReturnsError_AndNoSideEffects(string invalidPhone)
        {
            var dto = ValidDto();
            dto.PhoneNumber = invalidPhone;

            var (data, err) = await _service.CreateRescueRequestAsync(dto);

            Assert.IsNull(data);
            Assert.AreEqual("Phone number is required", err);

            _uowMock.VerifyNoOtherCalls();
            _cacheMock.VerifyNoOtherCalls();
            _kafkaMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task CreateRescueRequestAsync_ValidRequest_SavesDb_CacheAndKafka_ReturnsResponse()
        {
            var dto = ValidDto();

            // GenerateUniqueShortCodeAsync -> check collision:
            _rescueRequestRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RescueRequest, bool>>>()))
                .ReturnsAsync((RescueRequest?)null);

            RescueRequest? addedRequest = null;

            _rescueRequestRepoMock
                .Setup(r => r.AddAsync(It.IsAny<RescueRequest>()))
                .Callback<RescueRequest>(r =>
                {
                    // simulate EF sets Guid ID (thường là DB or app)
                    if (r.RescueRequestID == Guid.Empty)
                        r.RescueRequestID = Guid.NewGuid();

                    addedRequest = r;
                })
                .Returns(Task.CompletedTask);

            _rescueRequestImageRepoMock
                .Setup(r => r.AddAsync(It.IsAny<RescueRequestImage>()))
                .Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _cacheMock.Setup(c => c.RemoveAsync(ALL_KEY)).Returns(Task.CompletedTask);

            string? cachedKey = null;
            CreateRescueRequestResponseDTO? cachedValue = null;
            TimeSpan? cachedTtl = null;

            _cacheMock
                .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<CreateRescueRequestResponseDTO>(), It.IsAny<TimeSpan?>()))
                .Callback<string, CreateRescueRequestResponseDTO, TimeSpan?>((k, v, ttl) =>
                {
                    cachedKey = k;
                    cachedValue = v;
                    cachedTtl = ttl;
                })

                .Returns(Task.CompletedTask);
            _kafkaMock
                .Setup(k => k.ProduceAsync<RescueRequestKafkaMessage>(
                    KafkaSettings.RESCUE_REQUEST_CREATED_TOPIC,
                    It.Is<string>(s => IsGuid(s)), // Gọi hàm helper ở đây
                    It.IsAny<RescueRequestKafkaMessage>()))
                .Returns(Task.CompletedTask);

            var (data, err) = await _service.CreateRescueRequestAsync(dto);

            Assert.IsNull(err);
            Assert.IsNotNull(data);

            // Response basics
            Assert.AreNotEqual(Guid.Empty, data!.RescueRequestID);
            Assert.IsNotEmpty(data.ShortCode);
            Assert.AreEqual(dto.RequestType, data.RequestType);
            Assert.AreEqual(dto.PhoneNumber, data.CitizenPhone); // mapping ForMember PhoneNumber -> CitizenPhone
            Assert.AreEqual(RescueRequest_Status.PENDING_STATUS, data.Status);

            // ImageUrls: chỉ lấy URL hợp lệ
            Assert.AreEqual(2, data.ImageUrls.Count);

            // Verify DB
            _rescueRequestRepoMock.Verify(r => r.AddAsync(It.IsAny<RescueRequest>()), Times.Once);
            _rescueRequestImageRepoMock.Verify(r => r.AddAsync(It.IsAny<RescueRequestImage>()), Times.Exactly(2));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);

            // Verify cache
            _cacheMock.Verify(c => c.RemoveAsync(ALL_KEY), Times.Once);
            _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<CreateRescueRequestResponseDTO>(), It.IsAny<TimeSpan?>()), Times.Once);

            Assert.IsNotNull(cachedKey);
            StringAssert.StartsWith(PREFIX, cachedKey!);
            Assert.AreEqual(TimeSpan.FromMinutes(10), cachedTtl);
            Assert.IsNotNull(cachedValue);
            Assert.AreEqual(data.RescueRequestID, cachedValue!.RescueRequestID);

            // Verify kafka
            _kafkaMock.Verify(k => k.ProduceAsync(
                KafkaSettings.RESCUE_REQUEST_CREATED_TOPIC,
                It.IsAny<string>(),
                It.IsAny<RescueRequestKafkaMessage>()), Times.Once);

            // Sanity: entity actually created
            Assert.IsNotNull(addedRequest);
            Assert.AreNotEqual(Guid.Empty, addedRequest!.RescueRequestID);
            Assert.AreEqual(RescueRequest_Status.PENDING_STATUS, addedRequest.Status);
            Assert.IsNotEmpty(addedRequest.ShortCode);
        }

        [Test]
        public async Task CreateRescueRequestAsync_ImageUrlsContainsEmpty_SkipsEmptyImages()
        {
            var dto = ValidDto(new List<string> { "https://ok/1.png", "", "   ", "https://ok/2.png" });

            _rescueRequestRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RescueRequest, bool>>>()))
                .ReturnsAsync((RescueRequest?)null);

            _rescueRequestRepoMock
                .Setup(r => r.AddAsync(It.IsAny<RescueRequest>()))
                .Callback<RescueRequest>(r =>
                {
                    if (r.RescueRequestID == Guid.Empty)
                        r.RescueRequestID = Guid.NewGuid();
                })
                .Returns(Task.CompletedTask);

            _rescueRequestImageRepoMock.Setup(r => r.AddAsync(It.IsAny<RescueRequestImage>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _cacheMock.Setup(c => c.RemoveAsync(ALL_KEY)).Returns(Task.CompletedTask);
            _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<CreateRescueRequestResponseDTO>(), It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);

            _kafkaMock.Setup(k => k.ProduceAsync(
                KafkaSettings.RESCUE_REQUEST_CREATED_TOPIC,
                It.IsAny<string>(),
                It.IsAny<RescueRequestKafkaMessage>())).Returns(Task.CompletedTask);

            var (data, err) = await _service.CreateRescueRequestAsync(dto);

            Assert.IsNull(err);
            Assert.IsNotNull(data);
            Assert.AreEqual(2, data!.ImageUrls.Count);

            _rescueRequestImageRepoMock.Verify(r => r.AddAsync(It.IsAny<RescueRequestImage>()), Times.Exactly(2));
        }

        [Test]
        public async Task CreateRescueRequestAsync_SaveChangesFail_ReturnsError_AndDoesNotCacheOrKafka()
        {
            var dto = ValidDto();

            _rescueRequestRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RescueRequest, bool>>>()))
                .ReturnsAsync((RescueRequest?)null);
            // Setup giả lập hành vi DB
            _rescueRequestRepoMock
                .Setup(r => r.AddAsync(It.IsAny<RescueRequest>()))
                .Callback<RescueRequest>(r =>
                {
                    if (r.RescueRequestID == Guid.Empty)
                        r.RescueRequestID = Guid.NewGuid();
                })
                .Returns(Task.CompletedTask);

            _rescueRequestImageRepoMock.Setup(r => r.AddAsync(It.IsAny<RescueRequestImage>())).Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(0);

            var (data, err) = await _service.CreateRescueRequestAsync(dto);

            Assert.IsNull(data);
            Assert.AreEqual("Failed to create rescue request", err);

            _cacheMock.VerifyNoOtherCalls();
            _kafkaMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task CreateRescueRequestAsync_KafkaThrows_StillReturnsSuccess()
        {
            var dto = ValidDto();

            _rescueRequestRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RescueRequest, bool>>>()))
                .ReturnsAsync((RescueRequest?)null);

            _rescueRequestRepoMock
                .Setup(r => r.AddAsync(It.IsAny<RescueRequest>()))
                .Callback<RescueRequest>(r =>
                {
                    if (r.RescueRequestID == Guid.Empty)
                        r.RescueRequestID = Guid.NewGuid();
                })
                .Returns(Task.CompletedTask);

            _rescueRequestImageRepoMock.Setup(r => r.AddAsync(It.IsAny<RescueRequestImage>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _cacheMock.Setup(c => c.RemoveAsync(ALL_KEY)).Returns(Task.CompletedTask);
            _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<CreateRescueRequestResponseDTO>(), It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);

            _kafkaMock
                .Setup(k => k.ProduceAsync(
                    KafkaSettings.RESCUE_REQUEST_CREATED_TOPIC,
                    It.IsAny<string>(),
                    It.IsAny<RescueRequestKafkaMessage>()))
                .ThrowsAsync(new Exception("kafka down"));

            var (data, err) = await _service.CreateRescueRequestAsync(dto);

            Assert.IsNull(err);
            Assert.IsNotNull(data);

            _kafkaMock.Verify(k => k.ProduceAsync(
                KafkaSettings.RESCUE_REQUEST_CREATED_TOPIC,
                It.IsAny<string>(),
                It.IsAny<RescueRequestKafkaMessage>()), Times.Once);
        }

        // =========================
        // GetByShortCodeAsync
        // =========================

        [Test]
        public async Task GetByShortCodeAsync_CacheHit_ReturnsCached_NoDb()
        {
            var shortCode = "ABC123";
            var cachedDto = new CreateRescueRequestResponseDTO
            {
                RescueRequestID = Guid.NewGuid(),
                ShortCode = shortCode,
                ImageUrls = new List<string>()
            };

            _cacheMock
                .Setup(c => c.GetAsync<CreateRescueRequestResponseDTO>($"{PREFIX}{shortCode}"))
                .ReturnsAsync(cachedDto);

            var result = await _service.GetByShortCodeAsync(shortCode);

            Assert.IsNotNull(result);
            Assert.AreSame(cachedDto, result);

            _uowMock.VerifyNoOtherCalls();
            _rescueRequestRepoMock.VerifyNoOtherCalls();
            _rescueRequestImageRepoMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GetByShortCodeAsync_CacheMiss_DbHit_ReturnsAndCaches()
        {
            var shortCode = "ABC123";
            var requestId = Guid.NewGuid();

            _cacheMock
                .Setup(c => c.GetAsync<CreateRescueRequestResponseDTO>($"{PREFIX}{shortCode}"))
                .ReturnsAsync((CreateRescueRequestResponseDTO?)null);

            var entity = new RescueRequest
            {
                RescueRequestID = requestId,
                ShortCode = shortCode,
                IsDeleted = false,
                CitizenPhone = "0909",
                RequestType = RescueRequestType.RESCUE_TYPE,
                LocationLatitude = 10,
                LocationLongitude = 106
            };

            _rescueRequestRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RescueRequest, bool>>>()))
                .ReturnsAsync(entity);

            _rescueRequestImageRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<RescueRequestImage, bool>>>()))
                .ReturnsAsync(new List<RescueRequestImage>
                {
                    new RescueRequestImage { ImageUrl = "u1", RescueRequestID = requestId },
                    new RescueRequestImage { ImageUrl = "u2", RescueRequestID = requestId }
                });

            _cacheMock
                .Setup(c => c.SetAsync($"{PREFIX}{shortCode}", It.IsAny<CreateRescueRequestResponseDTO>(), It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GetByShortCodeAsync(shortCode);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result!.ImageUrls.Count);

            _cacheMock.Verify(c => c.SetAsync(
                $"{PREFIX}{shortCode}",
                It.IsAny<CreateRescueRequestResponseDTO>(),
                TimeSpan.FromMinutes(10)), Times.Once);
        }

        [Test]
        public async Task GetByShortCodeAsync_CacheMiss_DbMiss_ReturnsNull()
        {
            var shortCode = "ABC123";

            _cacheMock
                .Setup(c => c.GetAsync<CreateRescueRequestResponseDTO>($"{PREFIX}{shortCode}"))
                .ReturnsAsync((CreateRescueRequestResponseDTO?)null);

            _rescueRequestRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RescueRequest, bool>>>()))
                .ReturnsAsync((RescueRequest?)null);

            var result = await _service.GetByShortCodeAsync(shortCode);

            Assert.IsNull(result);

            _cacheMock.Verify(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<CreateRescueRequestResponseDTO>(),
                It.IsAny<TimeSpan?>()), Times.Never);
        }

        // =========================
        // GetAllRescueRequestsAsync
        // =========================

        [Test]
        public async Task GetAllRescueRequestsAsync_CacheHit_ReturnsCached_NoDb()
        {
            var cached = new List<CreateRescueRequestResponseDTO>
            {
                new CreateRescueRequestResponseDTO{ RescueRequestID = Guid.NewGuid(), ImageUrls = new List<string>() }
            };

            _cacheMock
                .Setup(c => c.GetAsync<List<CreateRescueRequestResponseDTO>>(ALL_KEY))
                .ReturnsAsync(cached);

            var result = await _service.GetAllRescueRequestsAsync();

            Assert.AreSame(cached, result);

            _uowMock.VerifyNoOtherCalls();
            _rescueRequestRepoMock.VerifyNoOtherCalls();
            _rescueRequestImageRepoMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GetAllRescueRequestsAsync_CacheMiss_DbHit_ReturnsAndCaches()
        {
            _cacheMock
                .Setup(c => c.GetAsync<List<CreateRescueRequestResponseDTO>>(ALL_KEY))
                .ReturnsAsync((List<CreateRescueRequestResponseDTO>?)null);

            var entities = new List<RescueRequest>
            {
                new RescueRequest { RescueRequestID = Guid.NewGuid(), IsDeleted = false, CitizenPhone="1", RequestType=RescueRequestType.RESCUE_TYPE, LocationLatitude=1, LocationLongitude=1, ShortCode="S1" },
                new RescueRequest { RescueRequestID = Guid.NewGuid(), IsDeleted = false, CitizenPhone="2", RequestType=RescueRequestType.SUPPLY_TYPE, LocationLatitude=2, LocationLongitude=2, ShortCode="S2" },
            };

            _rescueRequestRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<RescueRequest, bool>>>()))
                .ReturnsAsync(entities);

            _rescueRequestImageRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<RescueRequestImage, bool>>>()))
                .ReturnsAsync(new List<RescueRequestImage>());

            _cacheMock
                .Setup(c => c.SetAsync(ALL_KEY, It.IsAny<List<CreateRescueRequestResponseDTO>>(), It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GetAllRescueRequestsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            _cacheMock.Verify(c => c.SetAsync(
                ALL_KEY,
                It.IsAny<List<CreateRescueRequestResponseDTO>>(),
                TimeSpan.FromMinutes(5)), Times.Once);

            // 2 entities -> gọi lấy images 2 lần
            _rescueRequestImageRepoMock.Verify(
                r => r.GetAllAsync(It.IsAny<Expression<Func<RescueRequestImage, bool>>>()),
                Times.Exactly(2));
        }
        private bool IsGuid(string s)
        {
            return Guid.TryParse(s, out _);
        }
    }
}
