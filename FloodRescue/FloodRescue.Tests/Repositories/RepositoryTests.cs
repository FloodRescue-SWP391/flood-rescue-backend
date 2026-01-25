using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Implements;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace FloodRescue.Tests.Repositories
{
    [TestFixture]
    public class BaseRepositoryTests
    {
        // ===== KHAI BÁO BIẾN DÙNG CHUNG =====

        // DbContext dùng InMemory database (fake database trong RAM)
        private FloodRescueDbContext _context = null!;

        // Repository cần test - dùng Role entity làm ví dụ vì đơn giản
        private BaseRepository<Role> _repository = null!;

        // ===== SETUP - CHẠY TRƯỚC MỖI TEST =====
        [SetUp]
        public void Setup()
        {
            // Tạo options cho InMemory Database
            // Guid.NewGuid() để mỗi test có database riêng, tránh data conflict
            var options = new DbContextOptionsBuilder<FloodRescueDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Khởi tạo DbContext với InMemory Database
            _context = new FloodRescueDbContext(options);

            // Khởi tạo Repository với context vừa tạo
            _repository = new BaseRepository<Role>(_context);
        }

        // ===== TEARDOWN - CHẠY SAU MỖI TEST =====
        [TearDown]
        public void TearDown()
        {
            // Giải phóng tài nguyên, xóa database khỏi RAM
            _context.Dispose();
        }

        #region ===== TEST AddAsync =====

        [Test]
        public async Task AddAsync_WhenValidEntity_ShouldAddToDatabase()
        {
            await _context.Database.EnsureCreatedAsync();
            // ARRANGE - Chuẩn bị dữ liệu test
            // Tạo 1 Role mới để thêm vào database
            var newRole = new Role
            {
                RoleID = "TS",           // Test Role ID
                RoleName = "Test Role"   // Test Role Name
            };

            // ACT - Thực hiện hành động cần test
            // Gọi method AddAsync của repository
            await _repository.AddAsync(newRole);

            // Lưu thay đổi vào database (giống như UnitOfWork.SaveChangesAsync())
            await _context.SaveChangesAsync();

            // ASSERT - Kiểm tra kết quả
            // Đếm số record trong bảng Roles, phải = 5 (4 seed data + 1 mới thêm)
            var count = await _context.Roles.CountAsync();

            // Seed data có 4 roles (AD, RC, IM, RT) + 1 role mới = 5
            Assert.That(count, Is.EqualTo(5));
        }

        [Test]
        public async Task AddAsync_WhenCalled_ShouldNotSaveUntilSaveChanges()
        {
            // ARRANGE
            var newRole = new Role { RoleID = "NS", RoleName = "Not Saved" };

            // ACT - Chỉ gọi AddAsync, KHÔNG gọi SaveChanges
            await _repository.AddAsync(newRole);
            // Không gọi: await _context.SaveChangesAsync();

            // ASSERT - Kiểm tra entity chưa được lưu vào database thật
            // Entry().State cho biết trạng thái của entity
            var state = _context.Entry(newRole).State;

            // State phải là Added (đã thêm vào tracking nhưng chưa lưu DB)
            Assert.That(state, Is.EqualTo(EntityState.Added));
        }

        #endregion

        #region ===== TEST GetByIdAsync =====

        [Test]
        public async Task GetByIdAsync_WhenEntityExists_ShouldReturnEntity()
        {
            // ARRANGE - Seed data đã có Role "AD" từ DbContext.OnModelCreating
            // Không cần thêm data, chỉ cần ensure database được tạo
            await _context.Database.EnsureCreatedAsync();

            // ACT - Tìm Role với ID = "AD"
            var result = await _repository.GetByIdAsync("AD");

            // ASSERT
            // Kết quả không được null
            Assert.That(result, Is.Not.Null);

            // RoleName phải là "Admin" (theo seed data)
            Assert.That(result!.RoleName, Is.EqualTo("Admin"));
        }

        [Test]
        public async Task GetByIdAsync_WhenEntityNotExists_ShouldReturnNull()
        {
            // ARRANGE
            await _context.Database.EnsureCreatedAsync();

            // ACT - Tìm Role với ID không tồn tại
            var result = await _repository.GetByIdAsync("XX");

            // ASSERT - Kết quả phải là null
            Assert.That(result, Is.Null);
        }

        #endregion

        #region ===== TEST GetAsync (với filter) =====

        [Test]
        public async Task GetAsync_WhenFilterMatches_ShouldReturnFirstMatch()
        {
            // ARRANGE
            await _context.Database.EnsureCreatedAsync();

            // ACT - Tìm Role có RoleName chứa "Admin"
            // filter là Expression<Func<TEntity, bool>> - lambda expression
            var result = await _repository.GetAsync(r => r.RoleName == "Admin");

            // ASSERT
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.RoleID, Is.EqualTo("AD"));
        }

        [Test]
        public async Task GetAsync_WhenNoMatch_ShouldReturnNull()
        {
            // ARRANGE
            await _context.Database.EnsureCreatedAsync();

            // ACT - Tìm Role với điều kiện không match
            var result = await _repository.GetAsync(r => r.RoleName == "Not Exist");

            // ASSERT
            Assert.That(result, Is.Null);
        }

        #endregion

        #region ===== TEST GetAllAsync =====

        [Test]
        public async Task GetAllAsync_WhenNoFilter_ShouldReturnAllEntities()
        {
            // ARRANGE
            await _context.Database.EnsureCreatedAsync();

            // ACT - Lấy tất cả, không filter
            var result = await _repository.GetAllAsync();

            // ASSERT - Phải có 4 roles từ seed data
            Assert.That(result.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task GetAllAsync_WhenFilterApplied_ShouldReturnFilteredEntities()
        {
            // ARRANGE
            await _context.Database.EnsureCreatedAsync();

            // ACT - Lấy các Role có ID bắt đầu bằng "R"
            var result = await _repository.GetAllAsync(r => r.RoleID.StartsWith("R"));

            // ASSERT - Có 2 roles: RC và RT
            Assert.That(result.Count, Is.EqualTo(2));
        }

        #endregion

        #region ===== TEST Update =====

        [Test]
        public async Task Update_WhenCalled_ShouldModifyEntity()
        {
            // ARRANGE
            await _context.Database.EnsureCreatedAsync();

            // Lấy entity cần update
            var role = await _repository.GetByIdAsync("AD");

            // Thay đổi giá trị
            role!.RoleName = "Super Admin";

            // ACT - Gọi Update
            _repository.Update(role);
            await _context.SaveChangesAsync();

            // ASSERT - Lấy lại và kiểm tra
            var updated = await _repository.GetByIdAsync("AD");
            Assert.That(updated!.RoleName, Is.EqualTo("Super Admin"));
        }

        #endregion

        #region ===== TEST Delete =====

        [Test]
        public async Task Delete_WhenCalled_ShouldRemoveEntity()
        {
            // ARRANGE
            await _context.Database.EnsureCreatedAsync();
            var role = await _repository.GetByIdAsync("AD");

            // ACT - Xóa entity
            _repository.Delete(role!);
            await _context.SaveChangesAsync();

            // ASSERT - Kiểm tra entity đã bị xóa
            var deleted = await _repository.GetByIdAsync("AD");
            Assert.That(deleted, Is.Null);
        }

        #endregion
    }
}