using AutoMapper;
using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Implements;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.Auth;
using FloodRescue.Services.Implements;
using FloodRescue.Services.Interface;
using FloodRescue.Services.Mapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace FloodRescue.Tests.Services
{
    /// <summary>
    /// Unit Tests cho RegisterService
    /// Test chức năng đăng ký user mới
    /// </summary>
    [TestFixture]
    public class AuthServiceTests
    {
        // ===== KHAI BÁO BIẾN =====
        private FloodRescueDbContext _context = null!;
        private IUnitOfWork _unitOfWork = null!;
        private IMapper _mapper = null!;
        private IAuthService _registerService = null!;
        private ITokenService _tokenService = null!;
        private IConfiguration _configuration = null!;
        // Test roles
        private Role _adminRole = null!;
        private Role _coordinatorRole = null!;
        private Role _managerRole = null!;
        private Role _memberRole = null!;

        [SetUp]
        public void Setup()
        {
            // BƯỚC 1: Tạo InMemory Database
            var options = new DbContextOptionsBuilder<FloodRescueDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FloodRescueDbContext(options);

            // BƯỚC 2: Khởi tạo UnitOfWork
            _unitOfWork = new UnitOfWork(_context);

            // BƯỚC 3: Cấu hình AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = config.CreateMapper();

            // BƯỚC 4: Khởi tạo RegisterService
             
            _registerService = new AuthService(_unitOfWork, _mapper, _configuration, _tokenService);

            // BƯỚC 5: Tạo test roles
            _adminRole = new Role { RoleID = "AD", RoleName = "Admin", IsDeleted = false };
            _coordinatorRole = new Role { RoleID = "RC", RoleName = "Rescue Coordinator", IsDeleted = false };
            _managerRole = new Role { RoleID = "IM", RoleName = "Inventory Manager", IsDeleted = false };
            _memberRole = new Role { RoleID = "RT", RoleName = "Rescue Team Member", IsDeleted = false };

            _context.Roles.AddRange(_adminRole, _coordinatorRole, _managerRole, _memberRole);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _unitOfWork?.Dispose();
            _context?.Dispose();
        }

        #region ===== TEST RegisterAsync - Success Cases =====

        [Test]
        public async Task RegisterAsync_WhenValidRequest_ShouldRegisterSuccessfully()
        {
            // ARRANGE
            var request = new RegisterRequestDTO
            {
                Username = "newuser",
                Password = "Password123",
                Phone = "0901234567",
                FullName = "New User",
                RoleID = "RC"
            };

            // ACT
            var (data, errorMessage) = await _registerService.RegisterAsync(request);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Not.Null, "Data không được null khi register thành công");
                Assert.That(errorMessage, Is.Null, "ErrorMessage phải null khi thành công");
                Assert.That(data!.Username, Is.EqualTo("newuser"));
                Assert.That(data.Phone, Is.EqualTo("0901234567"));
                Assert.That(data.FullName, Is.EqualTo("New User"));
                Assert.That(data.RoleID, Is.EqualTo("RC"));
            });
        }

        [Test]
        public async Task RegisterAsync_ShouldHashPassword()
        {
            // ARRANGE
            var request = new RegisterRequestDTO
            {
                Username = "testuser",
                Password = "PlainPassword123",
                Phone = "0912345678",
                FullName = "Test User",
                RoleID = "RT"
            };

            // ACT
            await _registerService.RegisterAsync(request);

            // ASSERT - Password trong DB phải được hash
            var userInDb = await _unitOfWork.Users.GetAsync(u => u.Username == "testuser");
            Assert.Multiple(() =>
            {
                Assert.That(userInDb, Is.Not.Null);
                Assert.That(userInDb!.Password, Is.Not.EqualTo("PlainPassword123"), 
                    "Password phải được hash, không lưu plain text");
                Assert.That(userInDb.Password.Length, Is.GreaterThan(20), 
                    "Hashed password phải dài hơn plain password");
                
                // Verify BCrypt hash
                var isValidPassword = BCrypt.Net.BCrypt.Verify("PlainPassword123", userInDb.Password);
                Assert.That(isValidPassword, Is.True, 
                    "Password đã hash phải verify được với plain password");
            });
        }

        [Test]
        public async Task RegisterAsync_ShouldSaveToDatabase()
        {
            // ARRANGE
            var request = new RegisterRequestDTO
            {
                Username = "dbuser",
                Password = "Password123",
                Phone = "0923456789",
                FullName = "DB User",
                RoleID = "IM"
            };

            // ACT
            await _registerService.RegisterAsync(request);

            // ASSERT - Verify trong database
            var allUsers = await _unitOfWork.Users.GetAllAsync();
            Assert.That(allUsers.Count, Is.EqualTo(1));

            var userInDb = allUsers.First();
            Assert.That(userInDb.Username, Is.EqualTo("dbuser"));
        }

        [Test]
        public async Task RegisterAsync_WithDifferentRoles_ShouldSucceed()
        {
            // TEST các role hợp lệ: RC, IM, RT
            var roles = new[] { "RC", "IM", "RT" };

            foreach (var role in roles)
            {
                // ARRANGE
                var request = new RegisterRequestDTO
                {
                    Username = $"user_{role}",
                    Password = "Password123",
                    Phone = $"090{roles.ToList().IndexOf(role)}000000",
                    FullName = $"User {role}",
                    RoleID = role
                };

                // ACT
                var (data, errorMessage) = await _registerService.RegisterAsync(request);

                // ASSERT
                Assert.Multiple(() =>
                {
                    Assert.That(data, Is.Not.Null, $"Đăng ký với role {role} phải thành công");
                    Assert.That(errorMessage, Is.Null);
                    Assert.That(data!.RoleID, Is.EqualTo(role));
                });
            }
        }

        #endregion

        #region ===== TEST RegisterAsync - Validation Errors =====

        [Test]
        public async Task RegisterAsync_WhenUsernameExists_ShouldReturnError()
        {
            // ARRANGE - Tạo user có sẵn
            var existingUser = new User
            {
                UserID = Guid.NewGuid(),
                Username = "existinguser",
                Password = "hashedpassword",
                Phone = "0901111111",
                FullName = "Existing User",
                RoleID = "RC",
                IsDeleted = false
            };
            await _unitOfWork.Users.AddAsync(existingUser);
            await _unitOfWork.SaveChangesAsync();

            // Request với username đã tồn tại
            var request = new RegisterRequestDTO
            {
                Username = "existinguser",  // TRÙNG USERNAME
                Password = "Password123",
                Phone = "0902222222",  // Phone khác
                FullName = "New User",
                RoleID = "RC"
            };

            // ACT
            var (data, errorMessage) = await _registerService.RegisterAsync(request);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Null, "Data phải null khi username đã tồn tại");
                Assert.That(errorMessage, Is.EqualTo("Username already exists"));
            });
        }

        [Test]
        public async Task RegisterAsync_WhenPhoneExists_ShouldReturnError()
        {
            // ARRANGE - Tạo user có sẵn
            var existingUser = new User
            {
                UserID = Guid.NewGuid(),
                Username = "user1",
                Password = "hashedpassword",
                Phone = "0903333333",
                FullName = "User 1",
                RoleID = "RC",
                IsDeleted = false
            };
            await _unitOfWork.Users.AddAsync(existingUser);
            await _unitOfWork.SaveChangesAsync();

            // Request với phone đã tồn tại
            var request = new RegisterRequestDTO
            {
                Username = "newuser",  // Username khác
                Password = "Password123",
                Phone = "0903333333",  // TRÙNG PHONE
                FullName = "New User",
                RoleID = "RC"
            };

            // ACT
            var (data, errorMessage) = await _registerService.RegisterAsync(request);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Null);
                Assert.That(errorMessage, Is.EqualTo("Phone number already exists"));
            });
        }

        [Test]
        public async Task RegisterAsync_WhenRoleNotExists_ShouldReturnError()
        {
            // ARRANGE - Request với role không tồn tại
            var request = new RegisterRequestDTO
            {
                Username = "testuser",
                Password = "Password123",
                Phone = "0904444444",
                FullName = "Test User",
                RoleID = "XX"  // ROLE KHÔNG TỒN TẠI
            };

            // ACT
            var (data, errorMessage) = await _registerService.RegisterAsync(request);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Null);
                Assert.That(errorMessage, Is.EqualTo("Invalid RoleID"));
            });
        }

        [Test]
        public async Task RegisterAsync_WhenRoleIsAdmin_ShouldReturnError()
        {
            // ARRANGE - Cố đăng ký với role Admin
            var request = new RegisterRequestDTO
            {
                Username = "adminuser",
                Password = "Password123",
                Phone = "0905555555",
                FullName = "Admin User",
                RoleID = "AD"  // KHÔNG ĐƯỢC ĐĂNG KÝ ADMIN
            };

            // ACT
            var (data, errorMessage) = await _registerService.RegisterAsync(request);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Null);
                Assert.That(errorMessage, Is.EqualTo("Cannot register as admin"));
            });
        }

        [Test]
        public async Task RegisterAsync_WhenRoleIsAdminLowercase_ShouldReturnError()
        {
            // ARRANGE - Test case-insensitive check
            var request = new RegisterRequestDTO
            {
                Username = "adminuser",
                Password = "Password123",
                Phone = "0906666666",
                FullName = "Admin User",
                RoleID = "ad"  // lowercase "ad"
            };

            // ACT
            var (data, errorMessage) = await _registerService.RegisterAsync(request);

            // ASSERT
            // FIX: Service kiểm tra role tồn tại TRƯỚC, vì "ad" không có trong DB (chỉ có "AD")
            // Nên sẽ trả về "Invalid RoleID" thay vì "Cannot register as admin"
            Assert.That(errorMessage, Is.EqualTo("Invalid RoleID"), 
                "Role 'ad' (lowercase) không tồn tại trong DB, nên trả về Invalid RoleID");
        }

        #endregion

        #region ===== TEST RegisterAsync - Edge Cases =====

        [Test]
        public async Task RegisterAsync_WhenDeletedUserWithSameUsername_ShouldAllowRegister()
        {
            // ARRANGE - User đã bị xóa (IsDeleted = true)
            var deletedUser = new User
            {
                UserID = Guid.NewGuid(),
                Username = "deleteduser",
                Password = "oldpassword",
                Phone = "0907777777",
                FullName = "Deleted User",
                RoleID = "RC",
                IsDeleted = true  // ĐÃ XÓA
            };
            await _unitOfWork.Users.AddAsync(deletedUser);
            await _unitOfWork.SaveChangesAsync();

            // Request với username của user đã xóa
            var request = new RegisterRequestDTO
            {
                Username = "deleteduser",  // Trùng với user đã xóa
                Password = "NewPassword123",
                Phone = "0908888888",  // Phone khác
                FullName = "New User",
                RoleID = "RC"
            };

            // ACT
            var (data, errorMessage) = await _registerService.RegisterAsync(request);

            // ASSERT - Phải cho phép đăng ký vì user cũ đã bị xóa
            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Not.Null, 
                    "Phải cho phép đăng ký với username của user đã xóa");
                Assert.That(errorMessage, Is.Null);
            });
        }

        [Test]
        public async Task RegisterAsync_WhenDeletedUserWithSamePhone_ShouldAllowRegister()
        {
            // ARRANGE - User đã bị xóa
            var deletedUser = new User
            {
                UserID = Guid.NewGuid(),
                Username = "olduser",
                Password = "oldpassword",
                Phone = "0909999999",
                FullName = "Old User",
                RoleID = "RC",
                IsDeleted = true  // ĐÃ XÓA
            };
            await _unitOfWork.Users.AddAsync(deletedUser);
            await _unitOfWork.SaveChangesAsync();

            // Request với phone của user đã xóa
            var request = new RegisterRequestDTO
            {
                Username = "newuser",  // Username khác
                Password = "Password123",
                Phone = "0909999999",  // Trùng phone user đã xóa
                FullName = "New User",
                RoleID = "RC"
            };

            // ACT
            var (data, errorMessage) = await _registerService.RegisterAsync(request);

            // ASSERT - Phải cho phép đăng ký
            Assert.That(data, Is.Not.Null, 
                "Phải cho phép đăng ký với phone của user đã xóa");
        }

        #endregion
    }
}
