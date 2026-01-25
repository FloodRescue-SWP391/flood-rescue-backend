using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Implements;
using FloodRescue.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Tests.Repositories
{
    [TestFixture]
    public class UnitOfWorkTests
    {
        // ===== KHAI BÁO BIẾN =====
        private FloodRescueDbContext _context = null!;
        private UnitOfWork _unitOfWork = null!;
        private string _databaseName = null!;

        [SetUp]
        public void Setup()
        {
            _databaseName = Guid.NewGuid().ToString();  
            // Tạo InMemory Database cho mỗi test
            var options = new DbContextOptionsBuilder<FloodRescueDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;

            _context = new FloodRescueDbContext(options);

            // Khởi tạo UnitOfWork với context
            _unitOfWork = new UnitOfWork(_context);
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose UnitOfWork (sẽ dispose cả context bên trong)
            _unitOfWork.Dispose();
            _context.Dispose();
        }

        #region ===== TEST Repository Properties =====

        [Test]
        public void Roles_WhenAccessed_ShouldReturnSameInstance()
        {
            // ARRANGE & ACT
            // Truy cập property Roles 2 lần
            var firstAccess = _unitOfWork.Roles;
            var secondAccess = _unitOfWork.Roles;

            // ASSERT
            // Phải trả về cùng 1 instance (Singleton per UnitOfWork)
            // Điều này đảm bảo không tạo repository mới mỗi lần gọi
            Assert.That(firstAccess, Is.SameAs(secondAccess));
        }

        [Test]
        public void AllRepositories_ShouldNotBeNull()
        {
            // ASSERT - Tất cả repositories phải được khởi tạo đúng
            Assert.Multiple(() =>
            {
                Assert.That(_unitOfWork.Categories, Is.Not.Null);
                Assert.That(_unitOfWork.Users, Is.Not.Null);
                Assert.That(_unitOfWork.Roles, Is.Not.Null);
                Assert.That(_unitOfWork.RescueRequests, Is.Not.Null);
                Assert.That(_unitOfWork.ReliefOrders, Is.Not.Null);
                Assert.That(_unitOfWork.ReliefOrderDetails, Is.Not.Null);
                // ... các repository khác
            });
        }

        [Test]
        public void Repositories_ShouldImplementIBaseRepository()
        {
            // ASSERT - Kiểm tra type của repository
            Assert.Multiple(() =>
            {
                // Mỗi repository phải implement IBaseRepository<T>
                Assert.That(_unitOfWork.Roles, Is.InstanceOf<IBaseRepository<Role>>());
                Assert.That(_unitOfWork.Users, Is.InstanceOf<IBaseRepository<User>>());
                Assert.That(_unitOfWork.ReliefOrderDetails, Is.InstanceOf<IBaseRepository<ReliefOrderDetail>>());
            });
        }

        #endregion

        #region ===== TEST SaveChangesAsync =====

        [Test]
        public async Task SaveChangesAsync_WhenNoChanges_ShouldReturnZero()
        {
            // ACT - Gọi SaveChanges khi không có thay đổi gì
            var result = await _unitOfWork.SaveChangesAsync();

            // ASSERT - Trả về 0 (không có record nào bị ảnh hưởng)
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task SaveChangesAsync_WhenEntityAdded_ShouldReturnNumberOfChanges()
        {
            // ARRANGE - Thêm 1 entity mới
            var newRole = new Role { RoleID = "TT", RoleName = "Test" };
            await _unitOfWork.Roles.AddAsync(newRole);

            // ACT - Lưu thay đổi
            var result = await _unitOfWork.SaveChangesAsync();

            // ASSERT - Trả về 1 (1 record được thêm)
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public async Task SaveChangesAsync_WhenMultipleChanges_ShouldReturnTotalChanges()
        {
            // ARRANGE - Thêm 3 entities
            await _unitOfWork.Roles.AddAsync(new Role { RoleID = "T1", RoleName = "Test 1" });
            await _unitOfWork.Roles.AddAsync(new Role { RoleID = "T2", RoleName = "Test 2" });
            await _unitOfWork.Roles.AddAsync(new Role { RoleID = "T3", RoleName = "Test 3" });

            // ACT
            var result = await _unitOfWork.SaveChangesAsync();

            // ASSERT - Trả về 3
            Assert.That(result, Is.EqualTo(3));
        }

        #endregion

        #region ===== TEST Transaction Behavior =====

        [Test]
        public async Task UnitOfWork_ShouldMaintainSingleTransaction()
        {
            // Kịch bản: Thêm data vào 2 repositories khác nhau
            // Cả 2 phải được lưu trong cùng 1 transaction

            // ARRANGE
            var role = new Role { RoleID = "NR", RoleName = "New Role" };
            var warehouse = new Warehouse
            {
                WarehouseID = 999,
                Name = "Test Warehouse",
                Address = "Test Address"
            };

            // ACT - Thêm vào 2 repositories khác nhau
            await _unitOfWork.Roles.AddAsync(role);
            await _unitOfWork.Warehouses.AddAsync(warehouse);

            // Chỉ gọi SaveChangesAsync 1 LẦN cho tất cả
            var result = await _unitOfWork.SaveChangesAsync();

            // ASSERT
            // Cả 2 entity đều được lưu (result = 2)
            Assert.That(result, Is.EqualTo(2));

            // Verify cả 2 đều tồn tại trong DB
            var savedRole = await _unitOfWork.Roles.GetByIdAsync("NR");
            var savedWarehouse = await _unitOfWork.Warehouses.GetByIdAsync(999);

            Assert.Multiple(() =>
            {
                Assert.That(savedRole, Is.Not.Null);
                Assert.That(savedWarehouse, Is.Not.Null);
            });
        }

        [Test]
        public async Task UnitOfWork_WhenNotSaved_ChangesShouldNotPersist()
        {
            // Kịch bản: Thêm entity nhưng KHÔNG gọi SaveChangesAsync
            // Data không được lưu vào database

            // ARRANGE
            var role = new Role { RoleID = "NS", RoleName = "Not Saved" };

            // ACT - Chỉ Add, không Save
            await _unitOfWork.Roles.AddAsync(role);
            // KHÔNG gọi: await _unitOfWork.SaveChangesAsync();

            var state = _context.Entry(role).State;
            Assert.That(state, Is.EqualTo(EntityState.Added));

            // ASSERT - Tạo context mới để verify
            var options = new DbContextOptionsBuilder<FloodRescueDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;

            using var newContext = new FloodRescueDbContext(options);
            var found = await newContext.Roles.FindAsync("NS");

            // Entity không tồn tại vì chưa được SaveChanges
            Assert.That(found, Is.Null);
        }

        #endregion

        #region ===== TEST Dispose =====

        [Test]
        public void Dispose_WhenCalled_ShouldDisposeContext()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<FloodRescueDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new FloodRescueDbContext(options);
            var unitOfWork = new UnitOfWork(context);

            // ACT
            unitOfWork.Dispose();

            // ASSERT - Sau khi dispose, truy cập context sẽ throw exception
            Assert.Throws<ObjectDisposedException>(() =>
            {
                // Cố gắng truy cập context đã bị dispose
                _ = context.Roles.ToList();
            });
        }

        #endregion
    }
}