using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Implements;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.Implements;
using FloodRescue.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FloodRescue.Tests.Services
{
    /// <summary>
    /// Unit Tests cho TokenService
    /// Test các chức năng: GenerateToken, RefreshToken, RevokeAllTokens
    /// </summary>
    [TestFixture]
    public class TokenServiceTests
    {
        // ===== KHAI BÁO BIẾN DÙNG CHUNG =====

        // DbContext dùng InMemory database (fake database trong RAM)
        private FloodRescueDbContext _context = null!;

        // UnitOfWork để TokenService có thể truy cập database
        private IUnitOfWork _unitOfWork = null!;

        // IConfiguration để mock cấu hình JWT từ appsettings.json
        private IConfiguration _configuration = null!;

        // TokenService - service cần test
        private ITokenService _tokenService = null!;

        // User mẫu để test
        private User _testUser = null!;

        // Role mẫu để test
        private Role _testRole = null!;

        // ===== SETUP - CHẠY TRƯỚC MỖI TEST =====
        [SetUp]
        public void Setup()
        {
            // BƯỚC 1: Tạo InMemory Database
            // Guid.NewGuid() để mỗi test có database riêng, tránh data conflict
            var options = new DbContextOptionsBuilder<FloodRescueDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FloodRescueDbContext(options);

            // BƯỚC 2: Tạo mock IConfiguration
            // Giả lập cấu hình JWT giống như trong appsettings.json
            var configurationData = new Dictionary<string, string?>
            {
                // SecretKey phải >= 32 ký tự (256 bits) cho HmacSha256
                { "JwtSettings:SecretKey", "FloodRescue_TestSecretKey_2024_AtLeast32Characters!" },
                { "JwtSettings:Issuer", "FloodRescueAPI_Test" },
                { "JwtSettings:Audience", "FloodRescueClient_Test" },
                { "JwtSettings:AccessTokenExpirationMinutes", "15" },
                { "JwtSettings:RefreshTokenExpirationDays", "7" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            // BƯỚC 3: Khởi tạo UnitOfWork
            _unitOfWork = new UnitOfWork(_context);

            // BƯỚC 4: Khởi tạo TokenService với dependencies
            _tokenService = new TokenService(_configuration, _unitOfWork);

            // BƯỚC 5: Tạo Role mẫu (TokenService cần user.Role để tạo claims)
            _testRole = new Role
            {
                RoleID = "AD",
                RoleName = "Admin",
                IsDeleted = false
            };

            // BƯỚC 6: Tạo User mẫu để test
            _testUser = new User
            {
                UserID = Guid.NewGuid(),
                Username = "testuser",
                Password = "TestPassword123",
                Phone = "0123456789",
                FullName = "Test User",
                RoleID = "AD",
                Role = _testRole,  // QUAN TRỌNG: Gán Role cho User
                IsDeleted = false
            };
        }

        // ===== TEARDOWN - CHẠY SAU MỖI TEST =====
        [TearDown]
        public void TearDown()
        {
            // Giải phóng tài nguyên theo đúng thứ tự
            // UnitOfWork dispose trước (nó sẽ dispose context bên trong)
            _unitOfWork?.Dispose();
            // Context dispose sau (để đảm bảo clean up hoàn toàn)
            _context?.Dispose();
        }

        #region ===== TEST GenerateTokenAsync =====

        /// <summary>
        /// Test: Khi user hợp lệ, GenerateTokenAsync phải trả về cặp token không rỗng
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_WhenValidUser_ShouldReturnTokenPair()
        {
            // ARRANGE - User đã được tạo trong Setup()

            // ACT - Gọi method tạo token
            var (accessToken, refreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // ASSERT - Kiểm tra kết quả
            Assert.Multiple(() =>
            {
                // Access token không được null hoặc rỗng
                Assert.That(accessToken, Is.Not.Null.And.Not.Empty,
                    "Access token phải có giá trị");

                // Refresh token không được null hoặc rỗng
                Assert.That(refreshToken, Is.Not.Null.And.Not.Empty,
                    "Refresh token phải có giá trị");
            });
        }

        /// <summary>
        /// Test: Access Token phải có format JWT hợp lệ (3 phần ngăn cách bởi dấu chấm)
        /// JWT format: header.payload.signature
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_AccessToken_ShouldHaveValidJwtFormat()
        {
            // ARRANGE

            // ACT
            var (accessToken, _) = await _tokenService.GenerateTokenAsync(_testUser);

            // ASSERT
            // JWT có 3 phần: header.payload.signature
            var parts = accessToken.Split('.');
            Assert.That(parts.Length, Is.EqualTo(3),
                "JWT phải có 3 phần ngăn cách bởi dấu chấm (header.payload.signature)");
        }

        /// <summary>
        /// Test: Access Token phải chứa đúng claims của user
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_AccessToken_ShouldContainCorrectClaims()
        {
            // ARRANGE

            // ACT
            var (accessToken, _) = await _tokenService.GenerateTokenAsync(_testUser);

            // Giải mã JWT để đọc claims
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);

            // ASSERT - Kiểm tra các claims
            Assert.Multiple(() =>
            {
                // Claim "sub" (subject) phải là UserID
                var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                Assert.That(subClaim?.Value, Is.EqualTo(_testUser.UserID.ToString()),
                    "Claim 'sub' phải chứa UserID");

                // Claim "username" phải đúng
                var usernameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "username");
                Assert.That(usernameClaim?.Value, Is.EqualTo(_testUser.Username),
                    "Claim 'username' phải chứa Username của user");

                // Claim "fullName" phải đúng
                var fullNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "fullName");
                Assert.That(fullNameClaim?.Value, Is.EqualTo(_testUser.FullName),
                    "Claim 'fullName' phải chứa FullName của user");

                // Claim "jti" (JWT ID) phải tồn tại
                var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
                Assert.That(jtiClaim?.Value, Is.Not.Null.And.Not.Empty,
                    "Claim 'jti' phải tồn tại để liên kết với RefreshToken");

                // Claim "role" phải đúng
                var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                Assert.That(roleClaim?.Value, Is.EqualTo(_testRole.RoleName),
                    "Claim 'role' phải chứa RoleName của user");
            });
        }

        /// <summary>
        /// Test: Access Token phải có thời gian hết hạn đúng (15 phút)
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_AccessToken_ShouldExpireIn15Minutes()
        {
            // ARRANGE
            var beforeGenerate = DateTime.UtcNow;

            // ACT
            var (accessToken, _) = await _tokenService.GenerateTokenAsync(_testUser);
            var afterGenerate = DateTime.UtcNow;

            // Giải mã JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);

            // ASSERT
            // Token phải hết hạn trong khoảng 14-16 phút (cho phép sai số nhỏ)
            var expectedExpiry = beforeGenerate.AddMinutes(15);
            var tokenExpiry = jwtToken.ValidTo;

            // Kiểm tra thời gian hết hạn nằm trong khoảng hợp lý
            Assert.That(tokenExpiry, Is.GreaterThanOrEqualTo(beforeGenerate.AddMinutes(14)),
                "Token phải hết hạn sau ít nhất 14 phút");
            Assert.That(tokenExpiry, Is.LessThanOrEqualTo(afterGenerate.AddMinutes(16)),
                "Token phải hết hạn trong vòng 16 phút");
        }

        /// <summary>
        /// Test: Refresh Token phải được lưu vào database
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_RefreshToken_ShouldBeSavedToDatabase()
        {
            // ARRANGE

            // ACT + Lưu vào database
            var (_, refreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // ASSERT - Kiểm tra trong database
            var savedToken = await _unitOfWork.RefreshTokens.GetAsync(
                rt => rt.Token == refreshToken && rt.UserID == _testUser.UserID
            );

            Assert.Multiple(() =>
            {
                Assert.That(savedToken, Is.Not.Null,
                    "Refresh token phải được lưu vào database");
                Assert.That(savedToken!.IsUsed, Is.False,
                    "Refresh token mới tạo phải có IsUsed = false");
                Assert.That(savedToken.IsRevoked, Is.False,
                    "Refresh token mới tạo phải có IsRevoked = false");
                Assert.That(savedToken.UserID, Is.EqualTo(_testUser.UserID),
                    "Refresh token phải thuộc về user đúng");
            });
        }

        /// <summary>
        /// Test: Refresh Token phải có thời gian hết hạn 7 ngày
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_RefreshToken_ShouldExpireIn7Days()
        {
            // ARRANGE
            var beforeGenerate = DateTime.UtcNow;

            // ACT
            var (_, refreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // Lấy token từ database
            var savedToken = await _unitOfWork.RefreshTokens.GetAsync(rt => rt.Token == refreshToken);

            // ASSERT
            // Token phải hết hạn trong khoảng 6.9-7.1 ngày (cho phép sai số nhỏ)
            Assert.That(savedToken!.ExpiredAt, Is.GreaterThanOrEqualTo(beforeGenerate.AddDays(6.9)),
                "Refresh token phải hết hạn sau ít nhất 6.9 ngày");
            Assert.That(savedToken.ExpiredAt, Is.LessThanOrEqualTo(beforeGenerate.AddDays(7.1)),
                "Refresh token phải hết hạn trong vòng 7.1 ngày");
        }

        /// <summary>
        /// Test: Mỗi lần gọi GenerateTokenAsync phải tạo token khác nhau
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_WhenCalledMultipleTimes_ShouldReturnDifferentTokens()
        {
            // ARRANGE

            // ACT - Gọi 2 lần
            var (accessToken1, refreshToken1) = await _tokenService.GenerateTokenAsync(_testUser);
            var (accessToken2, refreshToken2) = await _tokenService.GenerateTokenAsync(_testUser);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(accessToken1, Is.Not.EqualTo(accessToken2),
                    "Access token phải khác nhau mỗi lần tạo (vì JwtId khác)");
                Assert.That(refreshToken1, Is.Not.EqualTo(refreshToken2),
                    "Refresh token phải khác nhau mỗi lần tạo (random)");
            });
        }

        /// <summary>
        /// Test: GenerateTokenAsync với user không có Role phải throw exception
        /// </summary>
        [Test]
        public void GenerateTokenAsync_WhenUserHasNoRole_ShouldThrowException()
        {
            // ARRANGE - User không có Role
            var userWithoutRole = new User
            {
                UserID = Guid.NewGuid(),
                Username = "noroleuser",
                Password = "TestPassword123",
                Phone = "0987654321",
                FullName = "No Role User",
                RoleID = "AD",
                Role = null,  // KHÔNG CÓ ROLE
                IsDeleted = false
            };

            // ACT & ASSERT
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _tokenService.GenerateTokenAsync(userWithoutRole);
            }, "Phải throw exception khi user không có Role");
        }

        #endregion

        #region ===== TEST RevokeAllTokensAsync =====

        /// <summary>
        /// Test: RevokeAllTokensAsync phải thu hồi tất cả refresh token của user
        /// </summary>
        [Test]
        public async Task RevokeAllTokensAsync_ShouldRevokeAllUserTokens()
        {
            // ARRANGE - Tạo nhiều refresh token cho user
            await _tokenService.GenerateTokenAsync(_testUser);
            await _tokenService.GenerateTokenAsync(_testUser);
            await _tokenService.GenerateTokenAsync(_testUser);

            // Verify có 3 token chưa bị revoke
            var tokensBeforeRevoke = await _unitOfWork.RefreshTokens.GetAllAsync(
                rt => rt.UserID == _testUser.UserID && !rt.IsRevoked
            );
            Assert.That(tokensBeforeRevoke.Count, Is.EqualTo(3),
                "Phải có 3 token trước khi revoke");

            // ACT - Thu hồi tất cả token
            await _tokenService.RevokeAllTokensAsync(_testUser.UserID);

            // ASSERT
            var tokensAfterRevoke = await _unitOfWork.RefreshTokens.GetAllAsync(
                rt => rt.UserID == _testUser.UserID && !rt.IsRevoked
            );
            Assert.That(tokensAfterRevoke.Count, Is.EqualTo(0),
                "Tất cả token phải bị thu hồi sau khi gọi RevokeAllTokensAsync");

            // Verify tất cả đều có IsRevoked = true
            var allTokens = await _unitOfWork.RefreshTokens.GetAllAsync(
                rt => rt.UserID == _testUser.UserID
            );
            Assert.That(allTokens.All(t => t.IsRevoked), Is.True,
                "Tất cả token phải có IsRevoked = true");
        }

        /// <summary>
        /// Test: RevokeAllTokensAsync không ảnh hưởng đến token của user khác
        /// </summary>
        [Test]
        public async Task RevokeAllTokensAsync_ShouldNotAffectOtherUsersTokens()
        {
            // ARRANGE - Tạo user thứ 2
            var otherUser = new User
            {
                UserID = Guid.NewGuid(),
                Username = "otheruser",
                Password = "OtherPassword123",
                Phone = "0111222333",
                FullName = "Other User",
                RoleID = "AD",
                Role = _testRole,
                IsDeleted = false
            };

            // Tạo token cho cả 2 user
            await _tokenService.GenerateTokenAsync(_testUser);
            await _tokenService.GenerateTokenAsync(otherUser);

            // ACT - Chỉ revoke token của _testUser
            await _tokenService.RevokeAllTokensAsync(_testUser.UserID);

            // ASSERT - Token của otherUser không bị ảnh hưởng
            var otherUserTokens = await _unitOfWork.RefreshTokens.GetAllAsync(
                rt => rt.UserID == otherUser.UserID && !rt.IsRevoked
            );
            Assert.That(otherUserTokens.Count, Is.EqualTo(1),
                "Token của user khác không được bị ảnh hưởng");
        }

        /// <summary>
        /// Test: RevokeAllTokensAsync với user không có token không throw exception
        /// </summary>
        [Test]
        public void RevokeAllTokensAsync_WhenUserHasNoTokens_ShouldNotThrowException()
        {
            // ARRANGE - User mới không có token nào

            // ACT & ASSERT - Không throw exception
            Assert.DoesNotThrowAsync(async () =>
            {
                await _tokenService.RevokeAllTokensAsync(_testUser.UserID);
            }, "Không được throw exception khi user không có token nào");
        }

        #endregion

        #region ===== TEST RefreshTokenAsync =====

        /// <summary>
        /// Test: RefreshTokenAsync với token hợp lệ phải trả về cặp token mới
        /// </summary>
        [Test]
        public async Task RefreshTokenAsync_WhenValidTokens_ShouldReturnNewTokenPair()
        {
            // ARRANGE - Thêm user vào database (để RefreshTokenAsync có thể tìm được)
            await _unitOfWork.Users.AddAsync(_testUser);
            await _unitOfWork.SaveChangesAsync();

            // Tạo token ban đầu
            var (oldAccessToken, oldRefreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // ACT - Refresh token
            var result = await _tokenService.RefreshTokenAsync(oldAccessToken, oldRefreshToken);

            // ASSERT
            Assert.That(result, Is.Not.Null,
                "Phải trả về cặp token mới khi refresh token hợp lệ");

            var (newAccessToken, newRefreshToken) = result!.Value;
            Assert.Multiple(() =>
            {
                Assert.That(newAccessToken, Is.Not.Null.And.Not.Empty,
                    "Access token mới phải có giá trị");
                Assert.That(newRefreshToken, Is.Not.Null.And.Not.Empty,
                    "Refresh token mới phải có giá trị");
                Assert.That(newAccessToken, Is.Not.EqualTo(oldAccessToken),
                    "Access token mới phải khác token cũ");
                Assert.That(newRefreshToken, Is.Not.EqualTo(oldRefreshToken),
                    "Refresh token mới phải khác token cũ");
            });
        }

        /// <summary>
        /// Test: RefreshTokenAsync phải đánh dấu refresh token cũ là đã sử dụng
        /// </summary>
        [Test]
        public async Task RefreshTokenAsync_ShouldMarkOldRefreshTokenAsUsed()
        {
            // ARRANGE
            await _unitOfWork.Users.AddAsync(_testUser);
            await _unitOfWork.SaveChangesAsync();

            var (oldAccessToken, oldRefreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // ACT
            await _tokenService.RefreshTokenAsync(oldAccessToken, oldRefreshToken);

            // ASSERT
            var usedToken = await _unitOfWork.RefreshTokens.GetAsync(rt => rt.Token == oldRefreshToken);
            Assert.That(usedToken!.IsUsed, Is.True,
                "Refresh token cũ phải được đánh dấu IsUsed = true");
        }

        /// <summary>
        /// Test: RefreshTokenAsync với refresh token đã sử dụng phải trả về null
        /// </summary>
        [Test]
        public async Task RefreshTokenAsync_WhenRefreshTokenAlreadyUsed_ShouldReturnNull()
        {
            // ARRANGE
            await _unitOfWork.Users.AddAsync(_testUser);
            await _unitOfWork.SaveChangesAsync();

            var (oldAccessToken, oldRefreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // Sử dụng refresh token lần đầu
            await _tokenService.RefreshTokenAsync(oldAccessToken, oldRefreshToken);

            // ACT - Cố gắng sử dụng lại refresh token đã dùng
            var result = await _tokenService.RefreshTokenAsync(oldAccessToken, oldRefreshToken);

            // ASSERT
            Assert.That(result, Is.Null,
                "Phải trả về null khi refresh token đã được sử dụng");
        }

        /// <summary>
        /// Test: RefreshTokenAsync với refresh token đã bị thu hồi phải trả về null
        /// </summary>
        [Test]
        public async Task RefreshTokenAsync_WhenRefreshTokenRevoked_ShouldReturnNull()
        {
            // ARRANGE
            await _unitOfWork.Users.AddAsync(_testUser);
            await _unitOfWork.SaveChangesAsync();

            var (oldAccessToken, oldRefreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // Thu hồi tất cả token
            await _tokenService.RevokeAllTokensAsync(_testUser.UserID);

            // ACT - Cố gắng sử dụng refresh token đã bị thu hồi
            var result = await _tokenService.RefreshTokenAsync(oldAccessToken, oldRefreshToken);

            // ASSERT
            Assert.That(result, Is.Null,
                "Phải trả về null khi refresh token đã bị thu hồi");
        }

        /// <summary>
        /// Test: RefreshTokenAsync với access token không hợp lệ phải trả về null
        /// </summary>
        [Test]
        public async Task RefreshTokenAsync_WhenInvalidAccessToken_ShouldReturnNull()
        {
            // ARRANGE
            await _unitOfWork.Users.AddAsync(_testUser);
            await _unitOfWork.SaveChangesAsync();

            var (_, validRefreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // ACT - Gửi access token giả
            var result = await _tokenService.RefreshTokenAsync("invalid.access.token", validRefreshToken);

            // ASSERT
            Assert.That(result, Is.Null,
                "Phải trả về null khi access token không hợp lệ");
        }

        /// <summary>
        /// Test: RefreshTokenAsync với refresh token không khớp với access token phải trả về null
        /// </summary>
        [Test]
        public async Task RefreshTokenAsync_WhenTokenPairMismatch_ShouldReturnNull()
        {
            // ARRANGE
            await _unitOfWork.Users.AddAsync(_testUser);
            await _unitOfWork.SaveChangesAsync();

            // Tạo 2 cặp token khác nhau
            var (accessToken1, _) = await _tokenService.GenerateTokenAsync(_testUser);
            var (_, refreshToken2) = await _tokenService.GenerateTokenAsync(_testUser);

            // ACT - Gửi access token từ cặp 1 với refresh token từ cặp 2
            var result = await _tokenService.RefreshTokenAsync(accessToken1, refreshToken2);

            // ASSERT
            Assert.That(result, Is.Null,
                "Phải trả về null khi access token và refresh token không khớp cặp");
        }

        /// <summary>
        /// Test: RefreshTokenAsync với refresh token hết hạn phải trả về null
        /// </summary>
        [Test]
        public async Task RefreshTokenAsync_WhenRefreshTokenExpired_ShouldReturnNull()
        {
            // ARRANGE
            await _unitOfWork.Users.AddAsync(_testUser);
            await _unitOfWork.SaveChangesAsync();

            var (oldAccessToken, oldRefreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // Giả lập token hết hạn bằng cách update ExpiredAt trong database
            var tokenInDb = await _unitOfWork.RefreshTokens.GetAsync(rt => rt.Token == oldRefreshToken);
            tokenInDb!.ExpiredAt = DateTime.UtcNow.AddDays(-1);  // Hết hạn 1 ngày trước
            _unitOfWork.RefreshTokens.Update(tokenInDb);
            await _unitOfWork.SaveChangesAsync();

            // ACT
            var result = await _tokenService.RefreshTokenAsync(oldAccessToken, oldRefreshToken);

            // ASSERT
            Assert.That(result, Is.Null,
                "Phải trả về null khi refresh token đã hết hạn");
        }

        #endregion

        #region ===== TEST Security Scenarios =====

        /// <summary>
        /// Test: Refresh Token phải đủ dài (đảm bảo entropy cao)
        /// Base64 của 64 bytes = 86 ký tự
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_RefreshToken_ShouldHaveSufficientLength()
        {
            // ARRANGE

            // ACT
            var (_, refreshToken) = await _tokenService.GenerateTokenAsync(_testUser);

            // ASSERT
            // 64 bytes => Base64 => khoảng 86 ký tự
            Assert.That(refreshToken.Length, Is.GreaterThanOrEqualTo(80),
                "Refresh token phải đủ dài để đảm bảo bảo mật (>= 80 ký tự)");
        }

        /// <summary>
        /// Test: Access Token có đúng Issuer và Audience không
        /// </summary>
        [Test]
        public async Task GenerateTokenAsync_AccessToken_ShouldHaveCorrectIssuerAndAudience()
        {
            // ARRANGE

            // ACT
            var (accessToken, _) = await _tokenService.GenerateTokenAsync(_testUser);

            // Giải mã JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(jwtToken.Issuer, Is.EqualTo("FloodRescueAPI_Test"),
                    "Issuer phải đúng với cấu hình");
                Assert.That(jwtToken.Audiences.First(), Is.EqualTo("FloodRescueClient_Test"),
                    "Audience phải đúng với cấu hình");
            });
        }

        #endregion
    }
}
